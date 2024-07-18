namespace Common.Authorization;

using Flurl;

public static class KeycloakUrls
{
    /// <summary>
    /// Returns the URL for OAuth token issuance.
    /// </summary>
    /// <param name="realmUrl">URL of the keycloak instance up to the realm name; I.e. "[base url]/auth/realms/[realm name]"</param>
    public static string Token(string realm, string realmUrl) => Url.Combine(realmUrl, realm, "protocol/openid-connect/token");

    /// <summary>
    /// Returns the URL for the OAuth well-known config.
    /// </summary>
    /// <param name="realmUrl">URL of the keycloak instance up to the realm name; I.e. "[base url]/auth/realms/[realm name]"</param>
    public static string WellKnownConfig(string realm, string realmUrl) => Url.Combine(realmUrl, realm, ".well-known/openid-configuration");

    public static string Authority(string realm, string realmUrl) => Url.Combine(realmUrl, realm);

}
