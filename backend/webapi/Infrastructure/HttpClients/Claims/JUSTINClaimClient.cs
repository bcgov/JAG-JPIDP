namespace Pidp.Infrastructure.HttpClients.Claims;

using Common.Exceptions;
using CommonModels.Models.JUSTIN;

public class JUSTINClaimClient(HttpClient httpClient, ILogger<JUSTINClaimClient> logger, PidpConfiguration configuration) : BaseClient(httpClient, logger), IJUSTINClaimClient
{

    public async Task<JUSTINClaimModel?> GetJustinClaims(string userPrincipalName)
    {

        var claimUrl = configuration.JustinClaimClient.Url;

        if (string.IsNullOrEmpty(claimUrl))
        {
            Logger.LogError("JustinClaimClient Url is not configured");
            throw new DIAMGeneralException("JustinClaimClient Url is not configured");
        }

        // todo - move this to configuration
        var response = await this.GetAsync<JUSTINClaimModel>($"{claimUrl}?email={userPrincipalName}");

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
