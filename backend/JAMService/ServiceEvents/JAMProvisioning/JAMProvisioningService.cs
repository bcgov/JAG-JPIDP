namespace JAMService.ServiceEvents.JAMProvisioning;

using System.Threading.Tasks;
using CommonModels.Models.JUSTIN;
using JAMService.Data;

public class JAMProvisioningService(JAMServiceDbContext context, ILogger<JAMProvisioningService> logger) : IJAMProvisioningService
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


        //   return Task.FromException(new DIAMGeneralException("Just testing!!"));

        return Task.CompletedTask;

    }

}
