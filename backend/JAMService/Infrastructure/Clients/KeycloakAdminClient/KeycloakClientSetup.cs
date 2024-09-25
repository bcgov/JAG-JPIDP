namespace JAMService.Infrastructure.Clients.KeycloakAdminClient;

using JAMService.Infrastructure.Clients.KeycloakClient;
using Keycloak.Net;

public static class KeycloakClientSetup
{
    public static IServiceCollection AddKeycloakClient(this IServiceCollection services, JAMServiceConfiguration configuration)
    {
        var client = new KeycloakClient(url: configuration.KeycloakConfiguration.BaseUrl, userName: configuration.KeycloakConfiguration.KeycloakAdminUser, password: configuration.KeycloakConfiguration.KeycloakAdminPassword, options: new KeycloakOptions(prefix: "auth", authenticationRealm: "master"));

        services.AddSingleton(client);

        services.AddScoped<IKeycloakService, KeycloakService>();

        return services;
    }
}
