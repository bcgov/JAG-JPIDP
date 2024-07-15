namespace Pidp.Infrastructure.HttpClients.Keycloak;

using System.Net;
using Common.Exceptions;
using global::Keycloak.Net.Models.RealmsAdmin;


// TODO Use DomainResult for success/fail?
public class KeycloakAdministrationClient : BaseClient, IKeycloakAdministrationClient
{
    public KeycloakAdministrationClient(HttpClient httpClient, ILogger<KeycloakAdministrationClient> logger) : base(httpClient, logger) { }

    public async Task<bool> AssignClientRole(string realm, Guid userId, string clientId, string roleName)
    {
        // We need both the name and ID of the role to assign it.
        var role = await this.GetClientRole(realm, clientId, roleName);
        if (role == null)
        {
            return false;
        }

        // Keycloak expects an array of roles.
        var result = await this.PostAsync($"{realm}/users/{userId}/role-mappings/clients/{role.ContainerId}", new[] { role });
        if (result.IsSuccess)
        {
            this.Logger.LogClientRoleAssigned(userId, roleName, clientId);
        }

        return result.IsSuccess;
    }
    public async Task<bool> AddGrouptoUser(string realm, Guid userId, string groupName)
    {
        var group = await this.GetRealmGroup(realm, groupName);
        if (group == null)
        {
            return false;
        }
        //assign user to group
        var response = await this.PutAsync($"{realm}/users/{userId}/groups/{group.Id}");
        if (!response.IsSuccess)
        {
            this.Logger.LogRealmGroupAssigned(userId, groupName);
        }
        return response.IsSuccess;

    }

    public async Task<bool> RemoveUserFromGroup(string realm, Guid userId, string groupName)
    {
        var group = await this.GetRealmGroup(realm, groupName);
        if (group == null)
        {
            return false;
        }
        //assign user to group
        var response = await this.DeleteAsync($"{realm}/users/{userId}/groups/{group.Id}");
        if (!response.IsSuccess)
        {
            this.Logger.LogRealmGroupRemoved(userId, groupName);
        }
        return response.IsSuccess;

    }

    public async Task<bool> AssignRealmRole(string realm, Guid userId, string roleName)
    {
        // We need both the name and ID of the role to assign it.
        var role = await this.GetRealmRole(realm, roleName);
        if (role == null)
        {
            return false;
        }

        // Keycloak expects an array of roles.
        var response = await this.PostAsync($"{realm}/users/{userId}/role-mappings/realm", new[] { role });
        if (response.IsSuccess)
        {
            this.Logger.LogRealmRoleAssigned(userId, roleName);
        }

        return response.IsSuccess;
    }

    public async Task<Client?> GetClient(string realm, string clientId)
    {
        var result = await this.GetAsync<IEnumerable<Client>>("clients");

        if (!result.IsSuccess)
        {
            return null;
        }

        var client = result.Value?.SingleOrDefault(c => c.ClientId == clientId);

        if (client == null)
        {
            this.Logger.LogClientNotFound(clientId);
        }

        return client;
    }

    public async Task<Role?> GetClientRole(string realm, string clientId, string roleName)
    {
        // Need ID of Client (not the same as ClientId!) to fetch roles.
        var client = await this.GetClient(realm, clientId);
        if (client == null)
        {
            return null;
        }

        var result = await this.GetAsync<IEnumerable<Role>>($"{realm}/clients/{client.Id}/roles");

        if (!result.IsSuccess)
        {
            return null;
        }

        var role = result.Value?.SingleOrDefault(r => r.Name == roleName);

        if (role == null)
        {
            this.Logger.LogClientRoleNotFound(roleName, clientId);
        }

        return role;
    }

    /// <summary>
    /// Get roles assigned to the user for the client Id
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public async Task<List<Role>?> GetUserClientRoles(string realm, Guid userId, Guid clientId)
    {


        var response = await this.GetAsync<List<Role>?>($"{realm}/users/{userId}/role-mappings/clients/{clientId}");
        return response.Value;
    }

    public async Task<Role?> GetRealmRole(string realm, string roleName)
    {
        var result = await this.GetAsync<Role>($"{realm}/roles/{WebUtility.UrlEncode(roleName)}");

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value;
    }

    public async Task<IdentityProvider> GetIdentityProvider(string realm, string name)
    {

        this.Logger.LogIDPLookup(realm, name);

        var result = await this.GetAsync<IdentityProvider>($"{realm}/identity-provider/instances/{name}");
        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value;
    }

    public async Task<Realm> GetRealm(string realm)
    {
        var result = await this.GetAsync<Realm>($"realms/{realm}");

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value;
    }

    public async Task<Group?> GetRealmGroup(string realm, string groupName)
    {
        var result = await this.GetAsync<IEnumerable<Group>>($"{realm}/groups?search={groupName}");

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value.SingleOrDefault();
    }

    public async Task<List<Group>?> GetUserGroups(string realm, Guid userId)
    {
        var result = await this.GetAsync<List<Group>>($"{realm}/users/{userId}/groups");

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value;
    }

    public async Task<bool> CreateUser(string realm, ExtendedUserRepresentation user)
    {
        var result = await this.PostAsync<string>($"{realm}/users", user);
        if (!result.IsSuccess)
        {
            return false;
        }

        return true;
    }


    public async Task<UserRepresentation?> GetUserByUsername(string realm, string username)
    {
        var result = await this.GetAsync<UserRepresentation>($"{realm}/users?username={username}");
        if (!result.IsSuccess)
        {
            throw new DIAMGeneralException($"Failed to get user by username [{string.Join(",", result.Errors)}].");
        }
        else if (result.IsSuccess && result.Value == null)
        {
            // if the user is missing we should get a null response but a successful one
            return null;
        }

        var userInfo = result.Value;

        return userInfo;
    }


    public async Task<UserRepresentation?> GetUser(string realm, Guid userId)
    {
        var result = await this.GetAsync<UserRepresentation>($"{realm}/users/{userId}");
        if (!result.IsSuccess)
        {
            return null;
        }

        var userInfo = result.Value;

        return userInfo;
    }

    public async Task<bool> RemoveClientRole(string realm, Guid userId, Role role)
    {
        if (role.ClientRole != true)
        {
            return false;
        }

        // Keycloak expects an array of roles.
        var response = await this.DeleteAsync($"{realm}/users/{userId}/role-mappings/clients/{role.ContainerId}", new[] { role });

        return response.IsSuccess;
    }

    public async Task<bool> UpdateUser(string realm, Guid userId, UserRepresentation userRep)
    {
        var result = await this.PutAsync($"{realm}/users/{userId}", userRep);
        return result.IsSuccess;
    }

    public async Task<bool> UpdateUser(string realm, Guid userId, Action<UserRepresentation> updateAction)
    {
        var user = await this.GetUser(realm, userId);
        if (user == null)
        {
            return false;
        }

        updateAction(user);

        return await this.UpdateUser(realm, userId, user);
    }

    public async Task<IEnumerable<IdentityProvider>> GetIdentityProviders(string realm)
    {
        var result = await this.GetAsync<IEnumerable<IdentityProvider>>($"{realm}/identity-provider/instances");
        if (!result.IsSuccess)
        {
            Serilog.Log.Error($"Failed to get identity providers [{string.Join(",", result.Errors)}].");
            return null;
        }

        return result.Value;
    }

    /// <summary>
    /// Extended info includes ID not typically returned
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    public async Task<ExtendedUserRepresentation?> GetExtendedUserByUsername(string realm, string username)
    {
        var result = await this.GetAsync<List<ExtendedUserRepresentation>>($"{realm}/users?username={username}");
        if (!result.IsSuccess)
        {
            return null;
        }

        var userInfo = result.Value.FirstOrDefault();

        return userInfo;
    }


    public async Task<bool> LinkUserToIdentityProvider(string realm, ExtendedUserRepresentation user, IdentityProvider idp)
    {
        var result = await this.PostAsync($"{realm}/users/{user.Id}/federated-identity/{idp.Alias}", new IdpLink()
        {
            UserId = user.Id.ToString(),
            UserName = user.Username
        }
            );
        if (result.IsSuccess)
        {
            this.Logger.LogUserLinkedToIdp(user.Id, idp.ProviderId);
            return true;
        }
        else
        {
            return false;
        }
    }

    private class IdpLink
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

}

public static partial class KeycloakAdministrationClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Could not find a Client with ClientId {clientId} in Keycloak response.")]
    public static partial void LogClientNotFound(this ILogger logger, string clientId);

    [LoggerMessage(2, LogLevel.Error, "Could not find a Client Role with name {roleName} from Client {clientId} in Keycloak response.")]
    public static partial void LogClientRoleNotFound(this ILogger logger, string roleName, string clientId);

    [LoggerMessage(3, LogLevel.Information, "User {userId} was assigned Role {roleName} in Client {clientId}.")]
    public static partial void LogClientRoleAssigned(this ILogger logger, Guid userId, string roleName, string clientId);

    [LoggerMessage(4, LogLevel.Information, "User {userId} was assigned Realm Role {roleName}.")]
    public static partial void LogRealmRoleAssigned(this ILogger logger, Guid userId, string roleName);
    [LoggerMessage(5, LogLevel.Information, "User {userId} was assigned Realm Group {groupName}.")]
    public static partial void LogRealmGroupAssigned(this ILogger logger, Guid userId, string groupName);
    [LoggerMessage(6, LogLevel.Information, "User {userId} was removed from Realm Group {groupName}.")]
    public static partial void LogRealmGroupRemoved(this ILogger logger, Guid userId, string groupName);
    [LoggerMessage(7, LogLevel.Information, "User {userId} was linked to IDP {idp}.")]
    public static partial void LogUserLinkedToIdp(this ILogger logger, Guid userId, string idp);
    [LoggerMessage(8, LogLevel.Information, "Getting {realm} IDP {idp}.")]
    public static partial void LogIDPLookup(this ILogger logger, string realm, string idp);

}
