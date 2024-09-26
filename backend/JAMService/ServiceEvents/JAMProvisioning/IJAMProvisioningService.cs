namespace JAMService.ServiceEvents.JAMProvisioning;

using CommonModels.Models.JUSTIN;

public interface IJAMProvisioningService
{
    public Task<Task> HandleJAMProvisioningRequest(string consumer, string key, JAMProvisioningRequestModel jAMProvisioningRequest);
}
