namespace DIAMCornetService.Infrastructure;

using System.Net.Http.Headers;
using System.Text;
using Common.Helpers.Extensions;
using DIAMCornetService.Services;
using Serilog;

public static class HttpClientConfiguration
{
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services, DIAMCornetServiceConfiguration config)
    {
        // use basic auth if username/password set
        if (!string.IsNullOrEmpty(config.CornetService.Username) && !string.IsNullOrEmpty(config.CornetService.Password))
        {
            Log.Logger.Information("Using CORNET endpoint {0}", config.CornetService.BaseAddress);

            services.AddHttpClientWithBaseAddress<ICornetORDSClient, CornetORDSClient>(config.CornetService.BaseAddress)
                 .ConfigureHttpClient(c => c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.CornetService.Username}:{config.CornetService.Password}"))));
        }

        return services;
    }

    public static IHttpClientBuilder AddHttpClientWithBaseAddress<TClient, TImplementation>(this IServiceCollection services, string baseAddress)
        where TClient : class
        where TImplementation : class, TClient
        => services.AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = new Uri(baseAddress.EnsureTrailingSlash()));
}
