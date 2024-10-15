namespace JAMService.ServiceEvents.JAMProvisioning;

using System.Threading.Tasks;
using Common.Constants.Auth;
using Common.Exceptions;
using Common.Kafka;
using Common.Models.Notification;
using CommonModels.Models.JUSTIN;
using Confluent.Kafka;
using DIAM.Common.Models;
using JAMService.Data;
using JAMService.Entities;
using JAMService.Infrastructure.Clients.KeycloakClient;
using JAMService.Infrastructure.HttpClients.JustinParticipant;
using NodaTime;
using Prometheus;

public class JAMProvisioningService(IClock clock, JAMServiceDbContext context, ILogger<JAMProvisioningService> logger, IJustinParticipantRoleClient justinClient, IKeycloakService keycloakService, IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer, IKafkaProducer<string, Notification> notificationProducer, JAMServiceConfiguration configuration) : IJAMProvisioningService
{

    private static readonly Histogram JAMProvisioningRequestDuration = Metrics.CreateHistogram("jam_provisioning_request_duration_seconds", "Duration of JAM Provisioning Request method");
    private static readonly Counter JAMProvisionRequests = Metrics.CreateCounter("jam_provisioning_requests_total", "Total number of JAM Provisioning Requests");
    private static readonly Counter JAMProvisionRequestFailures = Metrics.CreateCounter("jam_provisioning_request_failures_total", "Total number of JAM Provisioning Request Failures");

    public async Task<Task> HandleJAMProvisioningRequest(string consumer, string key, JAMProvisioningRequestModel jamProvisioningRequest)
    {


        using (JAMProvisioningRequestDuration.NewTimer())
        {
            var domainEvent = "jam-bcgov-provisioning-complete";
            var processStatus = "Complete";
            List<string> errorList = [];

            // check we havent handled this message before
            //check whether this message has been processed before   
            if (await context.HasBeenProcessed(key, consumer))
            {
                logger.LogWarning($"Message {key} has already been consumed by {consumer}");
                return Task.CompletedTask;
            }

            // get the app
            var app = context.Applications.FirstOrDefault(a => a.Name == jamProvisioningRequest.TargetApplication);

            if (app == null)
            {
                logger.LogError($"Application {jamProvisioningRequest.TargetApplication} not found in database");
                throw new DIAMConfigurationException($"Application {jamProvisioningRequest.TargetApplication} not found in database");
            }


            var sourceUser = await keycloakService.GetUserByUPN(jamProvisioningRequest.UPN, RealmConstants.BCPSRealm);

            if (sourceUser == null)
            {
                JAMProvisionRequestFailures.Inc();

                throw new DIAMUserProvisioningException($"Source user {jamProvisioningRequest.UPN} does not exist in realm {RealmConstants.BCPSRealm}");
            }

            logger.LogInformation($"Handling JAM Provisioning Request for {jamProvisioningRequest.PartyId} {jamProvisioningRequest.ParticipantId} Target app {jamProvisioningRequest.TargetApplication}");

            var appName = string.IsNullOrEmpty(app.JUSTINAppName) ? app.Name : app.JUSTINAppName;
            List<string> keycloakRoles = [];

            DbRoles dbRolesListing = null;
            if (jamProvisioningRequest.ParticipantId > 0)
            {
                // call JUSTIN ORDS endpoint to get dbRolesListing for user for requested application
                dbRolesListing = await justinClient.GetParticipantRolesByApplicationNameAndParticipantId(appName, jamProvisioningRequest.ParticipantId);
            }


            if (dbRolesListing == null && !string.IsNullOrEmpty(jamProvisioningRequest.UPN))
            {
                // call JUSTIN ORDS endpoint to get dbRolesListing for user for requested application
                dbRolesListing = await justinClient.GetParticipantRolesByApplicationNameAndUPN(appName, jamProvisioningRequest.UPN);
            }


            if (dbRolesListing != null)
            {
                logger.LogInformation($"User [{jamProvisioningRequest.UserId}] has roles [{dbRolesListing}]");
            }

            // get the keycloak mapping for these roles
            keycloakRoles = GetKeycloakRolesForJUSTINRoles(app, dbRolesListing);




            // if dbRolesListing are good - create or update user in Keycloak with appropriate client dbRolesListing

            // call keycloak to create or update user with dbRolesListing
            // User in BCPS is the original authenticated user
            logger.LogInformation($"User exists in keycloak BCPS Realm, checking user in ISB Realm");

            //User that is created or updated
            var existingUserinISB = await keycloakService.GetUserByUPN(jamProvisioningRequest.UPN, RealmConstants.ISBRealm);



            if (existingUserinISB == null)
            {

                if (dbRolesListing == null || dbRolesListing.Roles.Count == 0 || keycloakRoles.Count == 0)
                {
                    // publish error and mark account as errored
                    logger.LogWarning($"User [{jamProvisioningRequest.UserId}] does not exist - but has no mappable roles in JUSTIN for application {app} - marking request as errored");
                    errorList.Add("Unable to map keycloak roles to JUSTIN roles");
                    domainEvent = "jam-bcgov-provisioning-error";
                    processStatus = "Error";
                }
                else
                {
                    logger.LogInformation($"User {jamProvisioningRequest.ParticipantId} {jamProvisioningRequest.UserId} does not exist in keycloak ISB Realm, creating new user in ISB Realm");

                    existingUserinISB = await keycloakService.CreateNewUser(app, sourceUser, RealmConstants.ISBRealm);
                }
            }


            if (existingUserinISB != null)
            {
                if (dbRolesListing != null && dbRolesListing.Roles.Count > 0 && keycloakRoles.Count > 0)
                {
                    logger.LogInformation($"User {jamProvisioningRequest.ParticipantId} {jamProvisioningRequest.UserId} will be added to roles [{string.Join(",", dbRolesListing)}]");

                }
                else
                {
                    logger.LogWarning($"No roles found for {jamProvisioningRequest.PartyId} in {jamProvisioningRequest.TargetApplication} - user will be removed from all roles for app");
                    domainEvent = "jam-bcgov-account-disabled";
                }

                await keycloakService.UpdateUserApplicationRoles(existingUserinISB, app, keycloakRoles, RealmConstants.ISBRealm);


            }




            // send notification and completion or error response
            var msgKey = Guid.NewGuid().ToString();


            // SUJI  - produce a response - go to DomainEventResponseHandler in webapi
            var produceResponse = await processResponseProducer.ProduceAsync(configuration.KafkaCluster.ProcessResponseTopic, msgKey, new GenericProcessStatusResponse
            {
                DomainEvent = domainEvent,
                EventTime = clock.GetCurrentInstant(),
                Id = jamProvisioningRequest.AccessRequestId,
                Status = processStatus,
                ErrorList = errorList,
                PartId = "" + jamProvisioningRequest.ParticipantId
            });

            if (produceResponse.Status == PersistenceStatus.Persisted)
            {
                Serilog.Log.Information($"{msgKey} successfully published to {configuration.KafkaCluster.ProcessResponseTopic} partId is jamProvisioningRequest.ParticipantId");
            }
            else
            {
                JAMProvisionRequestFailures.Inc();

                Serilog.Log.Error($"Failed to published {msgKey} to {configuration.KafkaCluster.ProcessResponseTopic} partId is {jamProvisioningRequest.ParticipantId}");
                throw new DIAMGeneralException("Failed to published {msgKey} to {configuration.KafkaCluster.ProcessResponseTopic} partId is {jamProvisioningRequest.ParticipantId}");
            }


            // JESS
            // if complete response was sent then we'll send a notification email too
            if (produceResponse.Status == PersistenceStatus.Persisted)
            {
                msgKey = Guid.NewGuid().ToString();
                // create event data
                var eventData = new Dictionary<string, string>
                   {
                       { "firstName", sourceUser.FirstName },
                       { "application", app.Description},
                       { "appId", app.Name},
                        { "applicationUrl", app.LaunchUrl },
                       { "partyId", "" + jamProvisioningRequest.PartyId },
                       { "accessRequestId", "" + jamProvisioningRequest.AccessRequestId }
                   };
                var delivered = await notificationProducer.ProduceAsync(configuration.KafkaCluster.NotificationTopic, msgKey, new Notification
                {
                    To = sourceUser.Email,
                    DomainEvent = domainEvent,
                    EventData = eventData
                });

            }
            else
            {
                Serilog.Log.Error($"Sending failure response for user {jamProvisioningRequest.UserId} has not been completed [{produceResponse.Message}]");
                throw new DIAMGeneralException("Failed to publish {msgKey} to {configuration.KafkaCluster.NotificationTopic} partId is {jamProvisioningRequest.ParticipantId}");

            }


            // store key and consumer
            context.IdempotentConsumers.Add(
                new Common.Models.IdempotentConsumer()
                {
                    Consumer = consumer,
                    MessageId = key
                });

            var updates = await context.SaveChangesAsync();

            if (updates != 0)
            {
                logger.LogInformation($"Stored completed message {key} for {consumer}");
            }
            else
            {
                throw new DIAMGeneralException($"Failed to store message for {key} and {consumer}");
            }

            JAMProvisionRequests.Inc();
            return Task.CompletedTask;
        }
    }

    private List<string> GetKeycloakRolesForJUSTINRoles(Application app, DbRoles? dbRolesListing)
    {
        // go through app roles and find the keycloak roles
        // Filter the Application objects based on the matching roles
        var appRoles = context.AppRoleMappings.Where(appRoleMap => appRoleMap.Application == app).ToList();
        List<string> dbRolesList = [];

        if (dbRolesListing != null)
        {
            foreach (var dbRole in dbRolesListing.Roles)
            {
                dbRolesList.Add(dbRole.Dbroles);
            }
        }

        foreach (var appRoleMap in appRoles)
        {
            var sourceRoles = appRoleMap.SourceRoles;
            if (sourceRoles.SequenceEqual(dbRolesList))
            {
                logger.LogInformation($"Found matching roles for {appRoleMap.Description} for [{dbRolesListing}]");
                return appRoleMap.TargetRoles.ToList();
            }

        }

        return new List<string>();
    }
}
