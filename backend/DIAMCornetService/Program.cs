
namespace DIAMCornetService;

using System;
using System.Reflection;
using Asp.Versioning;
using Common.Logging;
using DIAMCornetService.Data;
using DIAMCornetService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load config
        var config = new DIAMCornetServiceConfiguration();
        builder.Configuration.Bind(config); // Bind configuration
        builder.Services.AddLogging(builder => builder.AddConsole());
        builder.Services.AddSingleton(config);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.ConfigureAuthorization(config);


        builder.Services.AddHttpClientServices(config);
        builder.Services.AddCornetServices(config);
        builder.Services.AddKafkaClients(config);
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        });

        builder.Services.AddDbContext<DIAMCornetDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.DIAMCornetDatabase, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        // Validate EF migrations on startup
        using (var serviceScope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<DIAMCornetDbContext>();
            try
            {
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                Log.Error($"Database migration failure {string.Join(",", ex.Message)}");
                throw;
            }
        }

        var name = Assembly.GetExecutingAssembly().GetName();
        var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Assembly", $"{name.Name}")
            .Enrich.WithProperty("Version", $"{name.Version}")
            .WriteTo.Console(
                outputTemplate: outputTemplate,
                theme: AnsiConsoleTheme.Code);


        if (!string.IsNullOrEmpty(config.SplunkConfig.Host))
        {
            loggerConfiguration.WriteTo.EventCollector(config.SplunkConfig.Host, config.SplunkConfig.CollectorToken);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        if (string.IsNullOrEmpty(config.SplunkConfig.Host))
        {
            Log.Warning("*** Splunk Host is not configured - check Splunk environment *** ");
        }
        else
        {
            Log.Information($"*** Splunk logging to {config.SplunkConfig.Host} ***");
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseAuthorization();
        app.MapMetrics();
        app.MapHealthChecks("/health/liveness").AllowAnonymous();

        app.MapControllers();

        app.Run();
    }
}
