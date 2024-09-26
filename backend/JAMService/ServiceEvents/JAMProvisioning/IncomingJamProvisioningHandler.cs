namespace JAMService.Features.JAMProvisioning;

using Common.Kafka;
using JAMService.ServiceEvents.JAMProvisioning;

public class IncomingJamProvisioningHandler(ILogger<IncomingJamProvisioningHandler> logger, IJAMProvisioningService service) : IKafkaHandler<string, CommonModels.Models.JUSTIN.JAMProvisioningRequestModel>
{

    public async Task<Task> HandleAsync(string consumerName, string key, CommonModels.Models.JUSTIN.JAMProvisioningRequestModel value)
    {

        logger.LogInformation($"Recieved JAM Provisioning Request for {value.PartyId} from {consumerName}");


        var response = service.HandleJAMProvisioningRequest(consumerName, key, value);




        return response;

    }
}
