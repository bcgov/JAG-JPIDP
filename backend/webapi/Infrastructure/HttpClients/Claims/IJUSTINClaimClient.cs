namespace Pidp.Infrastructure.HttpClients.Claims;

using CommonModels.Models.JUSTIN;

public interface IJUSTINClaimClient
{
    public Task<JUSTINClaimModel?> GetJustinClaims(string userPrincipalName);
}
