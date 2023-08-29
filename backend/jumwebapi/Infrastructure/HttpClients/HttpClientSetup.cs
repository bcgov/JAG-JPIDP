namespace jumwebapi.Infrastructure.HttpClients;

using System;
using System.Net.Http.Headers;
using System.Text;
using global::Common.Authorization;
using IdentityModel.Client;
using jumwebapi.Extensions;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;
using jumwebapi.Infrastructure.HttpClients.Keycloak;
using jumwebapi.Infrastructure.HttpClients.Mail;
using jumwebapi.Infrastructure.HttpClients.TestORDS;

public static class HttpClientSetup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, JumWebApiConfiguration config)
    {
        services.AddHttpClient<IAccessTokenClient, AccessTokenClient>();


        services.AddHttpClientWithBaseAddress<IChesClient, ChesClient>(config.ChesClient.Url)
            .WithBearerToken(new ChesClientCredentials
            {
                Address = config.ChesClient.TokenUrl,
                ClientId = config.ChesClient.ClientId,
                ClientSecret = config.ChesClient.ClientSecret
            });

        services.AddHttpClientWithBaseAddress<IKeycloakAdministrationClient, KeycloakAdministrationClient>(config.Keycloak.AdministrationUrl)
            .WithBearerToken(new KeycloakAdministrationClientCredentials
            {
                Address = config.Keycloak.TokenUrl,
                ClientId = config.Keycloak.AdministrationClientId,
                ClientSecret = config.Keycloak.AdministrationClientSecret
            });

        if (!string.IsNullOrEmpty(config.JustinAuthentication.ApiKey))
        {
            Serilog.Log.Logger.Information("JUSTIN Client configured with bearer token");
            services.AddHttpClientWithBaseAddress<IJustinParticipantClient, JustinParticipantClient>(config.JustinParticipantClient.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.JustinAuthentication.ApiKey));
            services.AddHttpClientWithBaseAddress<IJustinUserChangeManagementClient, JustinUserChangeManagementClient>(config.JustinChangeEventClient.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.JustinAuthentication.ApiKey));

        }
        //else if (config.JustinAuthentication.Method.Equals("oauth", StringComparison.OrdinalIgnoreCase) && !(string.IsNullOrEmpty(config.JustinAuthentication.ClientId) && string.IsNullOrEmpty(config.JustinAuthentication.ClientSecret) && string.IsNullOrEmpty(config.JustinAuthentication.TokenUrl)))
        //{




        //    Serilog.Log.Logger.Information($"JUSTIN Client configured with oauth with client {config.JustinAuthentication.ClientId}");

        //    services.AddHttpClientWithBaseAddress<IJustinParticipantClient, JustinParticipantClient>(config.JustinParticipantClient.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GetTokenString(config.JustinAuthentication.ClientId, config.JustinAuthentication.ClientSecret).Result));
        //    services.AddHttpClientWithBaseAddress<IJustinUserChangeManagementClient, JustinUserChangeManagementClient>(config.JustinChangeEventClient.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GetTokenString(config.JustinAuthentication.ClientId, config.JustinAuthentication.ClientSecret).Result));
        //}
        else if (config.JustinAuthentication.Method.Equals("basic", StringComparison.OrdinalIgnoreCase) && !(string.IsNullOrEmpty(config.JustinAuthentication.BasicAuthUsername) && string.IsNullOrEmpty(config.JustinAuthentication.BasicAuthPassword)))
        {
            Serilog.Log.Logger.Information($"JUSTIN Client configured with basic auth for user {config.JustinAuthentication.BasicAuthUsername}");
            var username = config.JustinAuthentication.BasicAuthUsername;
            var password = config.JustinAuthentication.BasicAuthPassword;
            var svcCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            services.AddHttpClientWithBaseAddress<IJustinParticipantClient, JustinParticipantClient>(config.JustinParticipantClient.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", svcCredentials));
            services.AddHttpClientWithBaseAddress<IJustinUserChangeManagementClient, JustinUserChangeManagementClient>(config.JustinChangeEventClient.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", svcCredentials));
        }
        else
        {
            Serilog.Log.Logger.Warning("JUSTIN Client configured with no authentication");
            services.AddHttpClientWithBaseAddress<IJustinParticipantClient, JustinParticipantClient>(config.JustinParticipantClient.Url);
            services.AddHttpClientWithBaseAddress<IJustinUserChangeManagementClient, JustinUserChangeManagementClient>(config.JustinChangeEventClient.Url);

        }

        var testOrds = Environment.GetEnvironmentVariable("TEST_ORDS");
        if (!string.IsNullOrEmpty(testOrds) && testOrds.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Serilog.Log.Information($"*** Adding test ORDS Http client - NON PRODUCTION USE ONLY! ***");
            var tokenService = new TokenService(config.JustinAuthentication.TokenUrl);
            services.AddHttpClientWithBaseAddress<ITestORDSClient, TestORDSClient>(config.TestORDSConfiguration.Url).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GetTokenString(config.JustinAuthentication.ClientId, config.JustinAuthentication.ClientSecret).Result));

            //services.AddHttpClientWithBaseAddress<ITestORDSClient, TestORDSClient>(config.TestORDSConfiguration.Url)
            //    .ConfigurePrimaryHttpMessageHandler(() =>
            //    {
            //        var handler = new HttpClientHandler();

            //        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

            //        return handler;
            //    });
            //; //.ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GetTokenString(config.JustinAuthentication.ClientId, config.JustinAuthentication.ClientSecret).Result));
        }


        services.AddTransient<ISmtpEmailClient, SmtpEmailClient>();

        // register background service for checking user changes in JUSTIN
        // services.AddHostedService<UserChangeBackgroundService>();



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
