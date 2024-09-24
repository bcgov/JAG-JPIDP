namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Constants.Auth;
using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;
using Keycloak.Net.Models.Users;

public class KeycloakService(KeycloakClient client) : IKeycloakService
{
    public async Task<User> GetUserByUPN(string userPrincipalName, string realm)
    {

        try
        {
            if (client != null)
            {

                var users = await client.GetUsersAsync(realm: realm,username: userPrincipalName);

                if(users.Count() == 1)
                {
                    return users.FirstOrDefault();
                }
                else
                {
                    Serilog.Log.Logger.Information($"User doesn't exist in Keycloak today with username{userPrincipalName}");
                }

            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Error getting users from keycloak {ex.Message}");
        }

        return null;
    }

    public async Task<User> CreateNewUser(User user, string realm)
    {
        User newUser = null;
        try
        {
            if(user != null)
            {
                user.Id = null;
                var createdUser = await client.CreateUserAsync(realm: realm, user: user);
                if (createdUser)
                {
                     newUser = await GetUserByUPN(user.UserName, realm);
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Failed to create user in keycloak {ex.Message}");
        }

        return newUser;
    }

    public async Task<User> UpdateUserApplicationRoles(User user, string applicationName, List<string> roles, string realm)
    {

    }
}
