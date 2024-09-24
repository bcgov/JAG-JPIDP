namespace JAMService.Infrastructure.Clients.KeycloakClient;

using Keycloak.Net.Models.Users;

public interface IKeycloakService
{
    public Task<User> GetUserByUPN(string userPrincipalName);

}
