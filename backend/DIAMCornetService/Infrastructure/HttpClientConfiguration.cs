namespace DIAMCornetService.Infrastructure;

using System.Net.Http.Headers;
using System.Text;
using DIAMCornetService.Services;

public static class HttpClientConfiguration
{
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services, DIAMCornetServiceConfiguration config)
    {
        // service with basic auth
        services.AddHttpClient<ICornetORDSClient, CornetORDSClient>(client =>
        {
            client.BaseAddress = new Uri(config.CornetService.BaseAddress);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.CornetService.Username}:{config.CornetService.Password}")));
        });

        return services;
    }
}
