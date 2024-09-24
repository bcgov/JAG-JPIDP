namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Threading.Tasks;
using Common.Constants.Auth;
using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;
using Keycloak.Net.Models.Users;

public class KeycloakService(KeycloakClient client) : IKeycloakService
{
    public async Task<User> GetUserByUPN(string userPrincipalName)
    {

        try
        {
            if (client != null)
            {

                var users = await client.GetUsersAsync(realm: RealmConstants.ISBRealm);

                Serilog.Log.Logger.Information($"got {users.Count()} users from keycloak");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Error getting users from keycloak {ex.Message}");
        }

        return null;
    }
}
