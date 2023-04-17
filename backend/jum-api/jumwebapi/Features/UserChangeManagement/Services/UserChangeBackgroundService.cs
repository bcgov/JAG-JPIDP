namespace jumwebapi.Features.UserChangeManagement.Services;

using System.Threading;
using Flurl.Util;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;
using Microsoft.AspNetCore.Mvc;
using Serilog;

/// <summary>
/// A Background service that polls JUSTIN looking for change events
/// Configured as a 
/// </summary>
public class UserChangeBackgroundService : BackgroundService
{
    private readonly IServiceProvider services;
    private readonly IJustinUserChangeManagementClient justinClient;
    private readonly JumWebApiConfiguration config;
    private readonly ILogger<UserChangeBackgroundService> logger;

    private readonly IJustinUserChangeService justinUserChangeService;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);


    public UserChangeBackgroundService(IServiceProvider services, JumWebApiConfiguration config, [FromServices] IJustinUserChangeManagementClient justinClient, IServiceScopeFactory scopeFactory, ILogger<UserChangeBackgroundService> logger)
    {
        Log.Information("*** Starting JUSTIN change event background service ***");
        this.services = services;
        this.justinClient = justinClient;
        this.config = config;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogDebug($"UserChangeBackgroundService is starting");

        var period = TimeSpan.FromSeconds(this.config.JustinChangeEventClient.PollRateSeconds);
        stoppingToken.Register(() => this.logger.LogDebug("UserChangeBackgroundService background task is stopping."));
        using var timer = new PeriodicTimer(period);
        using var scope = this.scopeFactory.CreateScope();

        while (!stoppingToken.IsCancellationRequested)
        {

            await this.semaphore.WaitAsync(stoppingToken);

            try
            {
                var userService = scope.ServiceProvider.GetRequiredService<JustinUserChangeService>();
                Log.Information("Running background task...");
                var changes = await this.justinClient.GetCurrentChangeEvents();
                if (changes?.Count() > 0)
                {
                    var response = await userService.ProcessChangeEvents(changes);
                }
            }
            finally
            {
                this.semaphore.Release();

            }

            // Wait (config value) seconds before running the task again
            await Task.Delay(TimeSpan.FromSeconds(this.config.JustinChangeEventClient.PollRateSeconds), stoppingToken);

        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync();
        try
        {
            await base.StopAsync(cancellationToken);
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}
