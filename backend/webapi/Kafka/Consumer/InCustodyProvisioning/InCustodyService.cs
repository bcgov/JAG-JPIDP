namespace Pidp.Kafka.Consumer.InCustodyProvisioning;

using Common.Constants.Auth;
using Common.Kafka;
using Common.Logging;
using Common.Models.CORNET;
using NodaTime;
using Pidp.Data;
using Pidp.Exceptions;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Models;
using Pidp.Models.Lookups;

public class InCustodyService(IClock clock, PidpDbContext context, ILogger<InCustodyService> logger, IKafkaProducer<string, AccessRequest> producer, IEdtCoreClient coreClient, IKeycloakAdministrationClient keycloakAdministrationClient, PidpConfiguration pidpConfiguration) : IInCustodyService
{

    public async Task<Task> ProcessInCustodySubmissionMessage(InCustodyParticipantModel value)
    {
        try
        {
            // check/add keycloak user
            var keycloakUser = await this.AddOrUpdateKeycloakUserAsync(value);

            // create access request
            var accessRequest = await this.CreateInCustodyAccessRequest(keycloakUser, value);

            // publish to disclosure portal
            var publishResponse = await this.PublishDisclosurePortalMessage(accessRequest, value);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogInCustodyServiceException(ex.Message, ex);
            throw;
        }

    }

    private async Task<ExtendedUserRepresentation> AddOrUpdateKeycloakUserAsync(InCustodyParticipantModel value)
    {
        try
        {
            // get the participant from the core service
            logger.EdtCoreParticipantLookup(value.ParticipantId, value.CSNumber);
            var participant = await coreClient.GetPersonByKey(value.ParticipantId);

            if (participant == null)
            {
                // log and return
                logger.LogParticipantNotFound(value.ParticipantId);
                throw new AccessRequestException($"No participant found in core with key {value.ParticipantId}");
            }

            logger.LogParticipantFound(value.ParticipantId, participant.FirstName, participant.LastName);

            // username will always be the cs number @ incustody.bcprosecution.gov.bc.ca
            var username = $"{value.CSNumber}@incustody.bcprosecution.gov.bc.ca";
            logger.LogCheckingKeycloakUser(username, value.ParticipantId);
            var keycloakUser = await keycloakAdministrationClient.GetExtendedUserByUsername("Corrections", username);

            if (keycloakUser == null)
            {
                logger.LogKeycloakUserNotPresent(username, value.ParticipantId);
                var createdUser = await keycloakAdministrationClient.CreateUser("Corrections", new ExtendedUserRepresentation()
                {
                    FirstName = participant.FirstName,
                    LastName = participant.LastName,
                    Username = username,
                    Email = username,
                    EmailVerified = true,
                    Enabled = true

                });

                if (createdUser)
                {
                    keycloakUser = await keycloakAdministrationClient.GetExtendedUserByUsername("Corrections", username);

                    if (keycloakUser == null)
                    {
                        logger.LogKeycloakUserCreationFailed(username, value.ParticipantId);
                        throw new AccessRequestException($"Failed to create keycloak user for {username}");
                    }
                    // get idp

                    var correctionsIdp = await keycloakAdministrationClient.GetIdentityProvider(RealmConstants.CorrectionsRealm, pidpConfiguration.CorrectionsIDP);
                    if (correctionsIdp != null)
                    {
                        // link the user to the corrections IDP
                        await keycloakAdministrationClient.LinkUserToIdentityProvider(RealmConstants.CorrectionsRealm, keycloakUser, correctionsIdp);
                        logger.LogKeycloakUserCreationSuccess(username, keycloakUser.Id.ToString(), value.ParticipantId);

                    }
                    else
                    {
                        logger.LogIDPNotFound(RealmConstants.CorrectionsRealm, pidpConfiguration.CorrectionsIDP);
                        throw new AccessRequestException($"Error processing keycloak user - PartID: {value.ParticipantId} - IDP not found");


                    }
                }
                else
                {
                    logger.LogKeycloakUserCreationFailed(username, value.ParticipantId);
                    throw new AccessRequestException($"Error processing keycloak user - PartID: {value.ParticipantId} - failed to create user");

                }


            }



            // return the user
            return keycloakUser;

        }
        catch (Exception ex)
        {
            logger.LogKeycloakServiceException(ex.Message, ex);
            throw new AccessRequestException($"Error processing keycloak user - PartID: {value.ParticipantId} {ex.Message}");
        }


    }


    private async Task<AccessRequest> CreateInCustodyAccessRequest(ExtendedUserRepresentation keycloakUser, InCustodyParticipantModel value)
    {


        var party = new Party
        {
            UserId = keycloakUser.Id,
            Jpdid = keycloakUser.Username,
            FirstName = keycloakUser.FirstName!,
            LastName = keycloakUser.LastName!,
            Email = keycloakUser.Username
        };

        context.Parties.Add(party);


        var partyAdded = await context.SaveChangesAsync();

        if (partyAdded > 0)
        {
            var accessRequest = new AccessRequest
            {
                Party = party,
                AccessTypeCode = AccessTypeCode.DigitalEvidenceDisclosure,
                RequestedOn = clock.GetCurrentInstant()
            };

            context.AccessRequests.Add(accessRequest);

            await context.SaveChangesAsync();

            return accessRequest;
        }
        else
        {
            throw new AccessRequestException($"Failed to create party for {keycloakUser.Username}");
        }


    }


    /// <summary>
    /// Publish a message to tell disclosure service that a new user should be created
    /// This will be with the username for corrections users
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<Task> PublishDisclosurePortalMessage(AccessRequest accessRequest, InCustodyParticipantModel value)
    {

        var msgId = Guid.NewGuid().ToString();
        // publish to the topic for disclosure portal to handle provisioning
        var delivered = await producer.ProduceAsync(pidpConfiguration.KafkaCluster.DisclosurePublicUserCreationTopic, msgId, accessRequest);

        if (delivered.Status == Confluent.Kafka.PersistenceStatus.Persisted)
        {
            logger.LogKafkaMsgSent(msgId, delivered.Partition.Value);
        }
        else
        {
            logger.LogKafkaMsgSendFailure(msgId, delivered.Status.ToString());
        }

        return Task.CompletedTask;
    }
}

public static partial class InCustodyServiceLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Looking up participant by key {key} CS#: [{csNumber}]")]
    public static partial void EdtCoreParticipantLookup(this ILogger logger, string key, string csNumber);
    [LoggerMessage(2, LogLevel.Warning, "Already processed message with key {key}")]
    public static partial void LogMessageAlreadyProcessed(this ILogger logger, string key);
    [LoggerMessage(3, LogLevel.Error, "Participant not found in core with key {key}")]
    public static partial void LogParticipantNotFound(this ILogger logger, string key);
    [LoggerMessage(4, LogLevel.Information, "Participant found {key} [{firstName} {lastName}]")]
    public static partial void LogParticipantFound(this ILogger logger, string key, string firstName, string lastName);
    [LoggerMessage(5, LogLevel.Information, "Checking for existing Keycloak user [{username}] PartID: {key}")]
    public static partial void LogCheckingKeycloakUser(this ILogger logger, string username, string key);
    [LoggerMessage(6, LogLevel.Information, "Keycloak user not present for [{username}] PartID: {key} - will be added")]
    public static partial void LogKeycloakUserNotPresent(this ILogger logger, string username, string key);
    [LoggerMessage(7, LogLevel.Information, "Keycloak user creation success [{username}] ID: {userId} PartID: {key} - will be added")]
    public static partial void LogKeycloakUserCreationSuccess(this ILogger logger, string userId, string username, string key);
    [LoggerMessage(8, LogLevel.Error, "Keycloak user creation failed [{username}]  PartID: {key} - check further logs")]
    public static partial void LogKeycloakUserCreationFailed(this ILogger logger, string username, string key);
    [LoggerMessage(9, LogLevel.Error, "Keycloak service error {msg}")]
    public static partial void LogKeycloakServiceException(this ILogger logger, string msg, Exception ex);
    [LoggerMessage(10, LogLevel.Warning, "Keycloak {realm} IDP not found {idp}")]
    public static partial void LogIDPNotFound(this ILogger logger, string realm, string idp);
    [LoggerMessage(11, LogLevel.Error, "Failed to complete in-custody onboarding {msg}")]
    public static partial void LogInCustodyServiceException(this ILogger logger, string msg, Exception ex);

}


