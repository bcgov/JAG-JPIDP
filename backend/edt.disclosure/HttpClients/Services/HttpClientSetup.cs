namespace edt.disclosure.HttpClients;

using System.Net.Http.Headers;
using Common.Authorization;
using Common.Constants.Auth;
using edt.disclosure.HttpClients.Keycloak;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using EdtDisclosureService.Extensions;
using IdentityModel.Client;
using Serilog;

public static class HttpClientSetup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, EdtDisclosureServiceConfiguration config)
    {
        services.AddHttpClient<IAccessTokenClient, AccessTokenClient>();

        Log.Logger.Information("Using EDT Disclosure endpoint {0}", config.EdtClient.Url);

        services.AddHttpClientWithBaseAddress<IEdtDisclosureClient, EdtDisclosureClient>(config.EdtClient.Url)
            .ConfigureHttpClient(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(config.DisclosureClientTimeoutSeconds);
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.EdtClient.ApiKey);
            });

        services.AddHttpClientWithBaseAddress<IKeycloakAdministrationClient, KeycloakAdministrationClient>(KeycloakUrls.Authority(realm: RealmConstants.BCPSRealm, config.Keycloak.AdministrationUrl))
    .WithBearerToken(new KeycloakAdministrationClientCredentials
    {
        Address = KeycloakUrls.Token(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl),
        ClientId = config.Keycloak.AdministrationClientId,
        ClientSecret = config.Keycloak.AdministrationClientSecret
    });

        return services;
    }

    public static IHttpClientBuilder AddHttpClientWithBaseAddress<TClient, TImplementation>(this IServiceCollection services, string baseAddress)
        where TClient : class
        where TImplementation : class, TClient
        => services.AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = new Uri(baseAddress.EnsureTrailingSlash()));

    public static IHttpClientBuilder WithBearerToken<T>(this IHttpClientBuilder builder, T credentials) where T : ClientCredentialsTokenRequest
    {
        builder.Services.AddSingleton(credentials)
            .AddTransient<BearerTokenHandler<T>>();

        builder.AddHttpMessageHandler<BearerTokenHandler<T>>();

        return builder;
    }
}
