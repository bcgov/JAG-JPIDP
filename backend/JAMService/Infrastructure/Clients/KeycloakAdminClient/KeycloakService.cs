namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Exceptions;
using JAMService.Data;
using JAMService.Entities;
using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;
using Keycloak.Net.Models.Groups;
using Keycloak.Net.Models.Users;

public class KeycloakService(ILogger<KeycloakService> logger,
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
                    var user = users.FirstOrDefault();
                    var userInfo = await client.GetUserAsync(realm: realm, userId: user.Id);

                    return userInfo;
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
    public async Task<User?> CreateNewUser(Application application, User sourceUser, string realm)
    {
        if (sourceUser == null)
        {
            throw new DIAMUserProvisioningException("Source user is null in call to CreateNewUser()");
        }
        User newUser = null;

        try
        {
            var sourceUserId = sourceUser.Id;

            var validIdp = application.ValidIDPs;

            var sourceFederatedId = sourceUser.FederatedIdentities.FirstOrDefault(id => validIdp.Contains(id.IdentityProvider));

            if (sourceFederatedId == null)
            {
                logger.LogError($"No federated ID found for user {sourceUser.UserName} - unable to provision account");
                throw new DIAMUserProvisioningException($"No federated ID found for user {sourceUser.UserName} - unable to provision account");
            }

            var mappedIdp = context.IDPMappers.Where(idp => idp.SourceRealm == "BCPS" && idp.SourceIdp == sourceFederatedId.IdentityProvider).FirstOrDefault();

            var targetIDPAlias = mappedIdp?.TargetIdp ?? sourceFederatedId.IdentityProvider;

            sourceUser.Id = null;
            sourceUser.FederationLink = null;

            var idp = await client.GetIdentityProviderAsync(realm: realm, identityProviderAlias: "azure-idir");

            //   sourceUser.FederationLink = application.DefaultIDPLink;


            var federatedId = new FederatedIdentity
            {
                IdentityProvider = mappedIdp.TargetIdp,
                UserId = sourceFederatedId.UserId,
                UserName = sourceFederatedId.UserName
            };

            sourceUser.FederatedIdentities = [federatedId];

            var createdUser = await client.CreateUserAsync(realm: realm, user: sourceUser);



            if (createdUser)
            {
                newUser = await this.GetUserByUPN(sourceUser.UserName, realm);
            }

        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error(ex, $"Failed to create user in keycloak {ex.Message}");
            throw new DIAMUserProvisioningException("Source user is null in call to CreateNewUser()");

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
    public async Task<User> UpdateUserApplicationRoles(User user, Application app, List<string> roles, string realm)
    {
        //user came in logged into diam user got created - for POR we have options POR_DELETE_ORDER, POR_READ_ONLY, POR_READ_WRITE
        try
        {
            // get all possible roles for app



            var availableAppRoles = app.RoleMappings.ToList().SelectMany(x => x.TargetRoles).Distinct().ToList();

            // get all roles that are not applicable to user
            var rolesNotGranted = availableAppRoles.Except(roles).ToList();

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
