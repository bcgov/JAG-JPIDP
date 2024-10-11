namespace JAMService.Infrastructure;

using System.Text;
using Common.Exceptions;
using Common.Helpers.Extensions;
using JAMService.Infrastructure.HttpClients.JustinParticipant;

public static class HttpClientConfiguration
{
    public static IServiceCollection ConfigureJUSTINHttpClient(this IServiceCollection services, JAMServiceConfiguration configuration)
    {

        if (configuration.JustinApplicationRolesClient.Method.Equals("basic", StringComparison.OrdinalIgnoreCase))
        {
            Serilog.Log.Logger.Information($"JUSTIN Client configured with basic auth for user {configuration.JustinApplicationRolesClient.BasicAuthUsername}");
            var username = configuration.JustinApplicationRolesClient.BasicAuthUsername;
            var password = configuration.JustinApplicationRolesClient.BasicAuthPassword;
            var svcCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            services.AddHttpClientWithBaseAddress<IJustinParticipantRoleClient, JustinParticipantRoleClient>(configuration.JustinApplicationRolesClient.ServiceUrl).ConfigureHttpClient(client => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", svcCredentials));


        }
        else
        {
            throw new DIAMGeneralException($"Only BASIC auth is currently supported");

        }

        return services;
    }

    public static IHttpClientBuilder AddHttpClientWithBaseAddress<TClient, TImplementation>(this IServiceCollection services, string baseAddress)
    where TClient : class
    where TImplementation : class, TClient
    => services.AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = new Uri(baseAddress.EnsureTrailingSlash()));


}
