namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Kafka;
using DIAM.Common.Models;
using JAMService.Data;
using JAMService.Entities;
using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;
using Keycloak.Net.Models.Groups;
using Keycloak.Net.Models.Users;

public class KeycloakService(ILogger<KeycloakService> logger,
    IKafkaProducer<string, GenericProcessStatusResponse> producer,
    JAMServiceConfiguration configuration, KeycloakClient client,
    JAMServiceDbContext context) : IKeycloakService
{




    /// <summary>
    /// 
    /// </summary>
    /// <param name="userPrincipalName"></param>
    /// <param name="realm"></param>
    /// <returns></returns>
    public async Task<User> GetUserByUPN(string userPrincipalName, string realm)
    {

        try
        {
            if (client != null)
            {

                var users = await client.GetUsersAsync(realm: realm, username: userPrincipalName);

                if (users.Count() == 1)
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


    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="realm"></param>
    /// <returns></returns>
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


    private async Task<Group>? GetChildGroup(string realm, Group parent, string childName)
    {
        try
        {
            var group = parent.Subgroups.FirstOrDefault(x => x.Name == childName);

            if (group == null)
            {
                var newGroup = await client.SetOrCreateGroupChildAsync(realm, parent.Id, new Group() { Name = childName });
                if (newGroup)
                {
                    group = await client.GetRealmGroupByPathAsync(realm: realm, path: parent.Path + "/" + childName);
                }
                else
                {
                    Serilog.Log.Logger.Warning($"Failed to create group {childName} in keycloak");
                }
            }

            return group;
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Failed to get or create group {childName} in keycloak {ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="application"></param>
    /// <param name="realm"></param>
    /// <returns></returns>
    private async Task<Group> GetAndAddGroup(string groupName, Application application, string realm)
    {

        Group? roleLevel = null;
        try
        {
            var rootPath = application.GroupPath.TrimStart('/').Split("/");

            var root = await client.GetRealmGroupByPathAsync(realm: realm, path: rootPath[0]);

            if (root == null)
            {
                Serilog.Log.Logger.Warning($"Root group {rootPath[0]} not found in keycloak");
                var createdRoot = await client.CreateGroupAsync(realm, new Group { Path = rootPath[0] });
                root = await client.GetRealmGroupByPathAsync(realm: realm, path: rootPath[0]);
            }

            if (root != null)
            {
                // second level
                var appLevel = await this.GetChildGroup(realm, root, rootPath[1]);

                // role level
                if (appLevel != null)
                {
                    roleLevel = await this.GetChildGroup(realm, appLevel, groupName);
                }

            }


        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Failed to get group in keycloak {ex.Message}");
        }

        return roleLevel;

    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="applicationName"></param>
    /// <param name="roles"></param>
    /// <param name="realm"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<User> UpdateUserApplicationRoles(User user, string applicationName, List<string> roles, string realm)
    {
        //user came in logged into diam user got created - for POR we have options POR_DELETE_ORDER, POR_READ_ONLY, POR_READ_WRITE
        try
        {
            // get all possible roles for app
            var app = context.Applications.FirstOrDefault(x => x.Name == applicationName);
            var allAppRoles = context.AppRoleMappings.Where(x => x.ApplicationId == app.Id).Select(x => x.Role).ToList();

            var groupMapping = new Dictionary<string, string>();

            // get all roles that are not applicable to user
            var rolesNotGranted = allAppRoles.Except(roles).ToList();

            foreach (var role in roles)
            {
                var group = await this.GetAndAddGroup(role, app, realm);

                if (group == null)
                {
                    throw new Exception($"Failed to get group {role} in keycloak");
                }
                else
                {
                    var updatedUser = await client.UpdateUserGroupAsync(realm: realm, userId: user.Id, groupId: group.Id, group: group);
                    if (updatedUser)
                    {
                        Serilog.Log.Information($"Added user {user.Id} to group {group.Name}");
                    }
                }
            }

            // get the roles for the user
            var userGroups = await client.GetUserGroupsAsync(realm: realm, userId: user.Id);

            foreach (var role in rolesNotGranted)
            {
                var userHasRole = userGroups.Any(x => x.Name == role);
                if (userHasRole)
                {
                    logger.LogInformation($"Removing role {role} from user {user.UserName}");
                    var deleted = await client.DeleteUserGroupAsync(realm: realm, userId: user.Id, groupId: userGroups.FirstOrDefault(x => x.Name == role).Id);
                    if (deleted)
                    {
                        logger.LogInformation($"Removed user {user.Id} {user.UserName} from group {role}");
                    }
                    else
                    {
                        logger.LogError($"Failed to remove user {user.Id} {user.UserName} from group {role}");

                    }
                }

            }

        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to update user {user.UserName} in keycloak {ex.Message}");
            throw;
        }

        return user;
    }
}
