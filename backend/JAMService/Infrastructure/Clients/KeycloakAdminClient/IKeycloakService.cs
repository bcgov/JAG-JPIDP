namespace JAMService.Infrastructure.Clients.KeycloakClient;

using Keycloak.Net.Models.Users;

public interface IKeycloakService
{
    public Task<User> GetUserByUPN(string userPrincipalName, string realm);
    public Task<User> CreateNewUser(User user, string realm);

    public Task<User> UpdateUserApplicationRoles(User user, string applicationName, List<string> roles, string realm);

}
