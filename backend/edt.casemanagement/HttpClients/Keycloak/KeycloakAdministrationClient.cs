

namespace edt.service.HttpClients.Keycloak;

using edt.casemanagement.HttpClients;
using EdtService.HttpClients.Keycloak;

public class KeycloakAdministrationClient : BaseClient, IKeycloakAdministrationClient
{

    public KeycloakAdministrationClient(HttpClient httpClient, ILogger<KeycloakAdministrationClient> logger) : base(httpClient, logger) { }


    /// <summary>
    /// Get Keycloak Identity Providers
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>IDPs</returns>
    public async Task<UserRepresentation?> GetUser(string realm, Guid userId)
    {
        var result = await this.GetAsync<UserRepresentation>($"{realm}/users/{userId}");
        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value;
    }

}

public static partial class KeycloakAdministrationClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Could not find a Client with ClientId {clientId} in Keycloak response.")]
    public static partial void LogClientNotFound(this ILogger logger, string clientId);

    [LoggerMessage(2, LogLevel.Error, "Could not find a Client Role with name {roleName} from Client {clientId} in Keycloak response.")]
    public static partial void LogClientRoleNotFound(this ILogger logger, string clientId, string roleName);

    [LoggerMessage(3, LogLevel.Information, "User {userId} was assigned Role {roleName} in Client {clientId}.")]
    public static partial void LogClientRoleAssigned(this ILogger logger, Guid userId, string clientId, string roleName);

    [LoggerMessage(4, LogLevel.Information, "User {userId} was assigned Realm Role {roleName}.")]
    public static partial void LogRealmRoleAssigned(this ILogger logger, Guid userId, string roleName);
}
