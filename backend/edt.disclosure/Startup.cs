
namespace edt.disclosure;

using System.Reflection;
using System.Text.Json;
using edt.disclosure.Data;
using edt.disclosure.HttpClients;
using edt.disclosure.Infrastructure.Telemetry;
using edt.disclosure.Kafka;
using edt.disclosure.ServiceEvents.CourtLocation.Handler;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        this.Configuration = configuration;
        StaticConfig = configuration;
    }

    public static IConfiguration StaticConfig { get; private set; }


    public void ConfigureServices(IServiceCollection services)
    {
        var config = this.InitializeConfiguration(services);

        if (string.IsNullOrEmpty(config.SchemaRegistry.Url))
        {
            Log.Error("Schema registry is not configured - please resolve configuration and retry");
            Environment.Exit(-1);
        }

        if (!string.IsNullOrEmpty(config.Telemetry.CollectorUrl))
        {

            var meters = new OtelMetrics();

            Action<ResourceBuilder> configureResource = r => r.AddService(
                 serviceName: TelemetryConstants.ServiceName,
                 serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                 serviceInstanceId: Environment.MachineName);

            Log.Logger.Information("Telemetry logging is enabled {0}", config.Telemetry.CollectorUrl);
            var resource = ResourceBuilder.CreateDefault().AddService(TelemetryConstants.ServiceName);

            services.AddOpenTelemetry()
               .ConfigureResource(configureResource)
               .WithTracing(builder =>
               {
                   builder.SetSampler(new AlwaysOnSampler())
                       .AddHttpClientInstrumentation()
                       .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
                       .AddAspNetCoreInstrumentation();

                   if (config.Telemetry.LogToConsole)
                   {
                       builder.AddConsoleExporter();
                   }

                   if (config.Telemetry.CollectorUrl != null)
                   {
                       builder.AddOtlpExporter(options =>
                       {
                           Log.Information("*** OpenTelemetry trace exporter enabled ***");

                           options.Endpoint = new Uri(config.Telemetry.CollectorUrl);
                           options.Protocol = OtlpExportProtocol.HttpProtobuf;
                       });
                   }
               })
               .WithMetrics(builder =>
                   builder.AddHttpClientInstrumentation()
                       .AddAspNetCoreInstrumentation()).StartWithHost();

        }


        services
          .AddAutoMapper(typeof(Startup))
          .AddKafkaConsumer(config)
          .AddHttpClients(config)


          .AddSingleton<IClock>(SystemClock.Instance)
          .AddSingleton<Microsoft.Extensions.Logging.ILogger>(svc => svc.GetRequiredService<ILogger<CourtLocationAccessRequestHandler>>());

        services.AddDbContext<DisclosureDataStoreDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.DisclosureDataStore, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.AddMediatR(typeof(Startup).Assembly);

        services.AddHealthChecks()
                .AddCheck("liveliness", () => HealthCheckResult.Healthy())
                .AddNpgSql(config.ConnectionStrings.DisclosureDataStore, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new KabobCaseParameterTransformer())))
             .AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<Startup>())
             .AddJsonOptions(options =>
             {
                 options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                 options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
             });
        services.AddHttpClient();

        services.AddSingleton<OtelMetrics>();



        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Disclosure Service API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,

                        },
                        new List<string>()
                    }
                });
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.CustomSchemaIds(x => x.FullName);
        });

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        services.AddKafkaConsumer(config);

        // Validate EF migrations on startup
        using (var serviceScope = services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<DisclosureDataStoreDbContext>();
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


        Log.Logger.Information("### EDT Disclosure Configuration complete");


    }
    private EdtDisclosureServiceConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new EdtDisclosureServiceConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information($"Counsel groups : {string.Join("|", config.EdtClient.DefenceCaseGroups.Split(",", StringSplitOptions.TrimEntries))}");

        Log.Logger.Information("### EDT Disclosure Service Version:{0} ###", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Logger.Debug("### Edt Disclosure Configuration:{0} ###", System.Text.Json.JsonSerializer.Serialize(config));

        return config;
    }
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

        }
        //app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseExceptionHandler("/error");
        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Disclosure Service API"));

        app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            //var userId = httpContext.User.GetUserId();
            //if (!userId.Equals(Guid.Empty))
            //{
            //    diagnosticContext.Set("User", userId);
            //}
        });
        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapMetrics();
            endpoints.MapHealthChecks("/health/liveness").AllowAnonymous();
        });

        app.UseMetricServer();
        app.UseHttpMetrics();

    }
}
