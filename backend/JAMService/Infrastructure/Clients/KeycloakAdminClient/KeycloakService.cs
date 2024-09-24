namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Threading.Tasks;
using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;
using Keycloak.Net.Models.Users;

public class KeycloakService(KeycloakClient client) : IKeycloakService
{
    public async Task<User> GetUserByUPN(string userPrincipalName)
    {


        return null;
    }
}
