namespace JAMService.ServiceEvents.JAMProvisioning;

using System.Threading.Tasks;
using Common.Exceptions;
using CommonModels.Models.JUSTIN;
using JAMService.Data;
using JAMService.Infrastructure.HttpClients.JustinParticipant;

public class JAMProvisioningService(JAMServiceDbContext context, ILogger<JAMProvisioningService> logger, IJustinParticipantRoleClient justinClient) : IJAMProvisioningService
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



        logger.LogInformation($"Handling JAM Provisioning Request for {jamProvisioningRequest.PartyId} Target app {jamProvisioningRequest.TargetApplication}");


        var participantIdDouble = double.Parse(jamProvisioningRequest.ParticipantId);
        List<string> roles = [];
        if (participantIdDouble > 0)
        {
            // call JUSTIN ORDS endpoint to get roles for user for requested application
            roles = await justinClient.GetParticipantRolesByApplicationNameAndParticipantId(jamProvisioningRequest.TargetApplication, participantIdDouble);
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
        }
        else
        {
            logger.LogWarning($"No roles found for {jamProvisioningRequest.PartyId} in {jamProvisioningRequest.TargetApplication}");
        }



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
