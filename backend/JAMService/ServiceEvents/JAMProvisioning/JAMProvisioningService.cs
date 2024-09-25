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
using JAMService.Infrastructure.Clients.KeycloakClient;
using JAMService.Infrastructure.HttpClients.JustinParticipant;
using NodaTime;

public class JAMProvisioningService(IClock clock, JAMServiceDbContext context, ILogger<JAMProvisioningService> logger, IJustinParticipantRoleClient justinClient, IKeycloakService keycloakService, IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer, IKafkaProducer<string, Notification> notificationProducer, JAMServiceConfiguration configuration) : IJAMProvisioningService
{
    public async Task<Task> HandleJAMProvisioningRequest(string consumer, string key, JAMProvisioningRequestModel jamProvisioningRequest)
    {




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
            throw new DIAMUserProvisioningException($"Source user {jamProvisioningRequest.UPN} does not exist in realm {RealmConstants.BCPSRealm}");
        }

        logger.LogInformation($"Handling JAM Provisioning Request for {jamProvisioningRequest.PartyId} {jamProvisioningRequest.ParticipantId} Target app {jamProvisioningRequest.TargetApplication}");


        List<string> roles = [];
        if (jamProvisioningRequest.ParticipantId > 0)
        {
            // call JUSTIN ORDS endpoint to get roles for user for requested application
            roles = await justinClient.GetParticipantRolesByApplicationNameAndParticipantId(jamProvisioningRequest.TargetApplication, jamProvisioningRequest.ParticipantId);
        }
        else
        {
            // call JUSTIN ORDS endpoint to get roles for user for requested application
            roles = await justinClient.GetParticipantRolesByApplicationNameAndUPN(jamProvisioningRequest.TargetApplication, jamProvisioningRequest.UPN);
        }



        roles.Add("POR_READ_ONLY");
        roles.Add("POR_READ_WRITE");


        // if roles are good - create or update user in Keycloak with appropriate client roles
        if (roles.Count != 0)
        {
            // call keycloak to create or update user with roles
            // User in BCPS is the original authenticated user


            logger.LogInformation($"User exists in keycloak BCPS Realm, checking user in ISB Realm");

            //User that is created or updated
            var existingUserinISB = await keycloakService.GetUserByUPN(jamProvisioningRequest.UPN, RealmConstants.ISBRealm);
            if (existingUserinISB == null)
            {
                logger.LogInformation($"User {jamProvisioningRequest.ParticipantId} {jamProvisioningRequest.UserId} does not exist in keycloak ISB Realm, creating new user in ISB Realm");

                existingUserinISB = await keycloakService.CreateNewUser(app, sourceUser, RealmConstants.ISBRealm);
            }
            if (existingUserinISB != null)
            {
                await keycloakService.UpdateUserApplicationRoles(existingUserinISB, jamProvisioningRequest.TargetApplication, roles, RealmConstants.ISBRealm);

                logger.LogInformation($"User roles have been updated");
            }

        }

        else
        {
            logger.LogWarning($"No roles found for {jamProvisioningRequest.PartyId} in {jamProvisioningRequest.TargetApplication}");
        }


        // send notification and completion or error response
        var msgKey = Guid.NewGuid().ToString();


        // SUJI  - produce a response - go to DomainEventResponseHandler in webapi
        var produceResponse = await processResponseProducer.ProduceAsync(configuration.KafkaCluster.ProcessResponseTopic, msgKey, new GenericProcessStatusResponse
        {
            DomainEvent = "jam-user-provisioning-complete",
            EventTime = clock.GetCurrentInstant(),
            Id = jamProvisioningRequest.AccessRequestId,
            Status = "Complete",
            PartId = "" + jamProvisioningRequest.ParticipantId
        });

        if (produceResponse.Status == PersistenceStatus.Persisted)
        {
            Serilog.Log.Information($"{msgKey} successfully published to {configuration.KafkaCluster.ProcessResponseTopic} partId is jamProvisioningRequest.ParticipantId");
        }
        else
        {
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
                DomainEvent = "jam-bcgov-usercreation-complete",
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


        return Task.CompletedTask;
    }

}
