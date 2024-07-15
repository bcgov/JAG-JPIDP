namespace Pidp.Infrastructure.HttpClients.Keycloak;

using global::Keycloak.Net.Models.RealmsAdmin;

public interface IKeycloakAdministrationClient
{
    /// <summary>
    /// Assigns a Client Role to the user, if it exists.
    /// Returns true if the operation was successful.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="clientId"></param>
    /// <param name="roleName"></param>
    Task<bool> AssignClientRole(string realm, Guid userId, string clientId, string roleName);

    /// <summary>
    /// Assigns a realm-level role to the user, if it exists.
    /// Returns true if the operation was successful.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleName"></param>
    Task<bool> AssignRealmRole(string realm, Guid userId, string roleName);

    /// <summary>
    /// Gets the Keycloak Client representation by ClientId.
    /// Returns null if unsuccessful.
    /// </summary>
    /// <param name="clientId"></param>
    Task<Client?> GetClient(string realm, string clientId);

    /// <summary>
    /// Gets the Keycloak Client Role representation by name.
    /// Returns null if unsuccessful or if no roles of that name exist on the client.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="roleName"></param>
    Task<Role?> GetClientRole(string realm, string clientId, string roleName);

    /// <summary>
    /// Gets the Keycloak Role representation by name.
    /// Returns null if unsuccessful.
    /// </summary>
    /// <param name="roleName"></param>
    Task<Role?> GetRealmRole(string realm, string roleName);

    /// <summary>
    /// Gets the Keycloak User Representation for the user.
    /// Returns null if unsuccessful.
    /// </summary>
    /// <param name="userId"></param>
    Task<UserRepresentation?> GetUser(string realm, Guid userId);


    /// <summary>
    /// Gets a keycloak user by username
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    Task<UserRepresentation?> GetUserByUsername(string realm, string username);

    Task<ExtendedUserRepresentation?> GetExtendedUserByUsername(string realm, string username);

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<bool> CreateUser(string realm, ExtendedUserRepresentation user);

    /// <summary>
    /// Removes the given Client Role from the User.
    /// Returns true if the operation was successful.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="role"></param>
    Task<bool> RemoveClientRole(string realm, Guid userId, Role role);

    /// <summary>
    /// Updates the User with the given Keycloak User Representation.
    /// Returns true if the operation was successful.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="userRep"></param>
    Task<bool> UpdateUser(string realm, Guid userId, UserRepresentation userRep);

    /// <summary>
    /// Fetches the User and updates with the given Action.
    /// Returns true if the operation was successful.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="updateAction"></param>
    Task<bool> UpdateUser(string realm, Guid userId, Action<UserRepresentation> updateAction);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    /// <returns></returns>
    Task<bool> AddGrouptoUser(string realm, Guid userId, string groupName);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="userId"></param>
    /// <param name="groupName"></param>
    /// <returns></returns>
    Task<bool> RemoveUserFromGroup(string realm, Guid userId, string groupName);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<Group>> GetUserGroups(string realm, Guid userId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="userId"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    Task<List<Role>?> GetUserClientRoles(string realm, Guid userId, Guid clientId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<Realm> GetRealm(string name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<IdentityProvider> GetIdentityProvider(string realm, string name);

    /// <summary>
    /// Get Identity providers within realm
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<IdentityProvider>> GetIdentityProviders(string realm);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="user"></param>
    /// <param name="idp"></param>
    /// <returns></returns>
    Task<bool> LinkUserToIdentityProvider(string realm, ExtendedUserRepresentation user, IdentityProvider idp);
}
