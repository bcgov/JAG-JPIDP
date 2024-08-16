namespace DIAMCornetService.Infrastructure;

using DIAMCornetService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NodaTime;
using Prometheus;

public static class ServiceConfiguration
{
    private static readonly string[] Tags = new[] { "diam-cornet-db" };

    /// <summary>
    /// Add CORNET Related services
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddCornetServices(this IServiceCollection services, DIAMCornetServiceConfiguration config)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICornetService, CornetService>();
        //    services.AddScoped<ICornetORDSClient, CornetORDSClient>();
        services.AddHealthChecks()
                .AddCheck("liveliness", () => HealthCheckResult.Healthy())
                .AddNpgSql(config.ConnectionStrings.DIAMCornetDatabase, tags: Tags).ForwardToPrometheus();
        return services;
    }
}
