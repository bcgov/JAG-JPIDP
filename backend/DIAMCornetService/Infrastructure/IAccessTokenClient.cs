namespace DIAMCornetService.Infrastructure;

using IdentityModel.Client;

public interface IAccessTokenClient
{
    Task<string> GetAccessTokenAsync(ClientCredentialsTokenRequest request);
}
