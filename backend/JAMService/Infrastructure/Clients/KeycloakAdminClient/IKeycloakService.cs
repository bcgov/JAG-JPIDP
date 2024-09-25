namespace JAMService.Infrastructure.Clients.KeycloakClient;

using JAMService.Entities;
using Keycloak.Net.Models.Users;

public interface IKeycloakService
{
    public Task<User> GetUserByUPN(string userPrincipalName, string realm);
    public Task<User?> CreateNewUser(Application application, User sourceUser, string realm);

    public Task<User> UpdateUserApplicationRoles(User user, string applicationName, List<string> roles, string realm);

}
