
namespace ISLInterfaces;

using System.Reflection;
using ISLInterfaces.Data;
using ISLInterfaces.Features.CaseAccess;
using ISLInterfaces.Infrastructure.Auth;
using ISLInterfaces.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Prometheus;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // keycloak controls auth
        builder.Services.AddKeycloakAuth(builder.Configuration);

        // db info
        var dbConnection = builder.Configuration.GetValue<string>("DatabaseConnectionInfo:DiamDatabase");
        var histogramAggregation = builder.Configuration.GetValue("HistogramAggregation", defaultValue: "explicit")!.ToLowerInvariant();

        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


        builder.Services.AddDbContext<DiamReadOnlyContext>(options => options
            .UseNpgsql(dbConnection, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        var otel = builder.Services.AddOpenTelemetry();
        otel.WithMetrics(metrics =>
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(Instrumentation.MeterName)
                .AddMeter("Microsoft.AspNetCore.Hosting")
        );


        // single service right now
        builder.Services.AddScoped<ICaseAccessService, CaseAccessService>();
        // metrics
        builder.Services.AddSingleton<Instrumentation>();

        builder.Host.UseSerilog((hostContext, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(hostContext.Configuration);
        });

        builder.Services.AddHealthChecks()
        .AddCheck("liveness", () => HealthCheckResult.Healthy()).ForwardToPrometheus();

        builder.Services.AddTransient<SerilogHandler>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        var loggerConfig = new LoggerConfiguration();

        var name = Assembly.GetExecutingAssembly().GetName();
        var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        var splunkHost = Environment.GetEnvironmentVariable("SplunkConfig__Host");
        splunkHost ??= builder.Configuration.GetValue<string>("SplunkConfig:Host");
        var splunkToken = Environment.GetEnvironmentVariable("SplunkConfig__CollectorToken");
        splunkToken ??= builder.Configuration.GetValue<string>("SplunkConfig:CollectorToken");

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Filter.ByExcluding("RequestPath like '/health%'")
            .Filter.ByExcluding("RequestPath like '/metrics%'")
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Assembly", $"{name.Name}")
            .Enrich.WithProperty("Version", $"{name.Version}")
            .WriteTo.Console(
                outputTemplate: outputTemplate,
                theme: AnsiConsoleTheme.Code
            );

        if (string.IsNullOrEmpty(splunkHost) || string.IsNullOrEmpty(splunkToken))
        {
            Console.WriteLine("Splunk Host or Token is not configured - check Splunk environment");
            Environment.Exit(-1);
        }

        Log.Information($"Logging to splunk host {splunkHost}");
        loggerConfig
            .MinimumLevel.Debug()
            .WriteTo.EventCollector("https://hec.monitoring.ag.gov.bc.ca:8088/services/collector", "d7811b9d-8079-4555-959e-81b547830c4d");


        Log.Logger = loggerConfiguration.CreateLogger();



        Log.Information($"Logging to splunk host {splunkHost}");




        Action<ResourceBuilder> configureResource = r => r.AddService(
             serviceName: TelemetryConstants.ServiceName,
             serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
             serviceInstanceId: Environment.MachineName);

        var resource = ResourceBuilder.CreateDefault().AddService(TelemetryConstants.ServiceName);

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.MapHealthChecks("/health/liveness").AllowAnonymous();
        app.MapMetrics("/metrics");

        app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
        });
        app.UseMetricServer();
        app.MapControllers();
        Log.Logger.Information("### ISL Interface Service running");

        app.Run();


    }

}
