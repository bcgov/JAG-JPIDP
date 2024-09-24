namespace JAMService.ServiceEvents.JAMProvisioning;

using System.Threading.Tasks;
using Common.Constants.Auth;
using Common.Exceptions;
using CommonModels.Models.JUSTIN;
using JAMService.Data;
using JAMService.Infrastructure.Clients.KeycloakClient;
using JAMService.Infrastructure.HttpClients.JustinParticipant;

public class JAMProvisioningService(JAMServiceDbContext context, ILogger<JAMProvisioningService> logger, IJustinParticipantRoleClient justinClient, IKeycloakService keycloakService) : IJAMProvisioningService
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




        // if roles are good - create or update user in Keycloak with appropriate client roles
        if (roles.Count != 0)
        {
            // call keycloak to create or update user with roles
            var existingUserInBCPS = await keycloakService.GetUserByUPN(jamProvisioningRequest.UPN, RealmConstants.BCPSRealm);
            if (existingUserInBCPS != null)
            {
                logger.LogInformation($"User exists in keycloak BCPS Realm, checking user in ISB Realm");

                var existingUserinISB = await keycloakService.GetUserByUPN(jamProvisioningRequest.UPN, RealmConstants.ISBRealm);
                if (existingUserinISB == null)
                {
                    logger.LogInformation($"User does not exist in keycloak ISB Realm, creating new user in ISB Realm");

                   existingUserinISB = await keycloakService.CreateNewUser(existingUserinISB, RealmConstants.ISBRealm);
       
                }
                if(existingUserinISB != null)
                {

                }

            }
            else
            {
                throw new DIAMAuthException("User does not exist in BCPS Realm");
            }
        }
        else
        {
            logger.LogWarning($"No roles found for {jamProvisioningRequest.PartyId} in {jamProvisioningRequest.TargetApplication}");
        }


        // send notification and completion or error response





        // store key and consumer
        //context.IdempotentConsumers.Add(
        //    new Common.Models.IdempotentConsumer()
        //    {
        //        Consumer = consumer,
        //        MessageId = key
        //    });

        //var updates = await context.SaveChangesAsync();

        //if (updates != 0)
        //{
        //    logger.LogInformation($"Stored completed message {key} for {consumer}");
        //}
        //else
        //{
        //    throw new DIAMGeneralException($"Failed to store message for {key} and {consumer}");
        //}

        return Task.FromException(new DIAMGeneralException("testing"));

    }

}
