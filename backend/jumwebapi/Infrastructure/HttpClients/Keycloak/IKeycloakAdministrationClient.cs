namespace jumwebapi.Infrastructure.HttpClients.Keycloak;

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
    /// Returns null if unccessful.
    /// </summary>
    /// <param name="clientId"></param>
    Task<Client?> GetClient(string realm, string clientId);

    /// <summary>
    /// Gets the Keycloak Client Role representation by name.
    /// Returns null if unccessful or if no roles of that name exist on the client.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="roleName"></param>
    Task<Role?> GetClientRole(string realm, string clientId, string roleName);

    /// <summary>
    /// Gets the Keycloak Role representation by name.
    /// Returns null if unccessful.
    /// </summary>
    /// <param name="roleName"></param>
    Task<Role?> GetRealmRole(string realm, string roleName);

    /// <summary>
    /// Gets the Keycloak User Representation for the user.
    /// Returns null if unccessful.
    /// </summary>
    /// <param name="userId"></param>
    Task<UserRepresentation?> GetUser(string realm, Guid userId);

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

    Task<IEnumerable<IdentityProvider>> IdentityProviders(string realm);
}
