namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Threading.Tasks;
using Keycloak.Net.Models.Users;

public class KeycloakService(KeycloakClient client) : IKeycloakService
{
    public Task<User> GetUserByUPN(string userPrincipalName)
    {


    }
}
