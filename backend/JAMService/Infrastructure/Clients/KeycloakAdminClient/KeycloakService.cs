namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Constants.Auth;
using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;
using Keycloak.Net.Models.Groups;
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
            if (user != null)
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
    private async Task<Group> GetAndAddGroup(string groupName, string realm)
    {
        try
        {
            var group = await client.GetRealmGroupByPathAsync(realm, path: "JAM/POR/POR_READ_ONLY");

            if (group == null)
            {
                var createdGroup = await client.CreateGroupAsync(realm, new Keycloak.Net.Models.Groups.Group { Name = groupName });

                if (!createdGroup)
                {
                    Serilog.Log.Logger.Error($"Failed to create group");
                }
                else
                {
                    group = await client.GetGroupAsync(realm, groupName);
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Failed to get group in keycloak {ex.Message}");
        }

        return group;

    }
    public async Task<User> UpdateUserApplicationRoles(User user, string applicationName, List<string> roles, string realm)
    {
        //user came in logged into diam user got created - for POR we have options POR_DELETE_ORDER, POR_READ_ONLY, POR_READ_WRITE

        var groupMapping = new Dictionary<string, string>();
        foreach (var role in roles)
        {
            var group = await this.GetAndAddGroup(role, realm);

            if (group == null)
            {
                throw new Exception($"Failed to get group {role} in keycloak");
            }
            else
            {
                var updatedUser = await client.UpdateUserGroupAsync(realm, user.Id, group.Id, group);
            }
        }

        return user;
    }
}
