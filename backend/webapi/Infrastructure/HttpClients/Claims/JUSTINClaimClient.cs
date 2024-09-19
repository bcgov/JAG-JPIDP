namespace Pidp.Infrastructure.HttpClients.Claims;

using CommonModels.Models.JUSTIN;

public class JUSTINClaimClient(HttpClient httpClient, ILogger<JUSTINClaimClient> logger) : BaseClient(httpClient, logger), IJUSTINClaimClient
{

    public async Task<JUSTINClaimModel?> GetJustinClaims(string userPrincipalName)
    {
        var response = await this.GetAsync<JUSTINClaimModel>($"https://custom-claim-api-bfc5f3-dev.apps.emerald.devops.gov.bc.ca/api/participants?email={userPrincipalName}");

        if (response.IsSuccess)
        {
            return response.Value;
        }
        else
        {
            Logger.LogError($"Failed to get JUSTIN claims for {userPrincipalName}");
            return null;
        }
    }
}
