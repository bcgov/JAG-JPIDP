namespace edt.casemanagement.HttpClients;

using System.Net.Http.Headers;
using edt.casemanagement.HttpClients.Services.EdtCore;
using edt.service.HttpClients.Keycloak;
using EdtService.Extensions;
using EdtService.HttpClients.Keycloak;
using IdentityModel.Client;
using Serilog;

public static class HttpClientSetup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, EdtServiceConfiguration config)
    {
        services.AddHttpClient<IAccessTokenClient, AccessTokenClient>();

        //services.AddHttpClientWithBaseAddress<IAddressAutocompleteClient, AddressAutocompleteClient>(config.AddressAutocompleteClient.Url);

        Log.Logger.Information("Using EDT endpoint {0}", config.EdtClient.Url);

        services.AddHttpClientWithBaseAddress<IEdtClient, EdtClient>(config.EdtClient.Url)
            .ConfigureHttpClient(c => c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.EdtClient.ApiKey));

        services.AddHttpClientWithBaseAddress<IKeycloakAdministrationClient, KeycloakAdministrationClient>(config.Keycloak.AdministrationUrl)
    .WithBearerToken(new KeycloakAdministrationClientCredentials
    {
        Address = config.Keycloak.TokenUrl,
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
