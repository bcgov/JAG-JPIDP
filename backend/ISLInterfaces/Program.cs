
namespace ISLInterfaces;

using System.Reflection;
using ISLInterfaces.Data;
using ISLInterfaces.Features.CaseAccess;
using ISLInterfaces.Infrastructure.Auth;
using ISLInterfaces.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Prometheus;
using Serilog;

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



        builder.Services.AddDbContext<DiamReadOnlyContext>(options => options
            .UseNpgsql(dbConnection, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        var otel = builder.Services.AddOpenTelemetry();
        otel.WithMetrics(metrics =>
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }


        Action<ResourceBuilder> configureResource = r => r.AddService(
             serviceName: TelemetryConstants.ServiceName,
             serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
             serviceInstanceId: Environment.MachineName);

        var resource = ResourceBuilder.CreateDefault().AddService(TelemetryConstants.ServiceName);

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.MapHealthChecks("/health/liveness");
        app.MapMetrics("/metrics");

        app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
        });
        app.UseMetricServer();
        app.MapControllers();

        app.Run();
    }

}
