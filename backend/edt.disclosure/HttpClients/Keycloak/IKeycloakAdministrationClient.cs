namespace edt.disclosure.HttpClients.Keycloak;

public interface IKeycloakAdministrationClient
{


    /// <summary>
    /// Gets the Keycloak User Representation for the user.
    /// Returns null if unccessful.
    /// </summary>
    /// <param name="userId"></param>
    Task<UserRepresentation?> GetUser(Guid userId);

}
