
namespace edt.service;

using System.Reflection;
using System.Text.Json;
using edt.service.Data;
using edt.service.HttpClients;
using edt.service.Infrastructure.Telemetry;
using edt.service.Kafka;
using edt.service.ServiceEvents.UserAccountCreation.ConsumerRetry;
using edt.service.ServiceEvents.UserAccountCreation.Handler;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using NodaTime;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using Azure.Monitor.OpenTelemetry.Exporter;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Prometheus;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using FluentValidation.AspNetCore;
using NodaTime.Serialization.SystemTextJson;
using edt.service.ServiceEvents.UserAccountModification.Handler;
using edt.service.Infrastructure.Auth;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
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


        var workAroundTopic = Environment.GetEnvironmentVariable("USER_CREATION_PLAINTEXT_TOPIC");
        if (!string.IsNullOrEmpty(workAroundTopic))
        {
            Log.Information("*** Plain text user creation topic is configured {0} ***", workAroundTopic);
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
                   if (config.Telemetry.AzureConnectionString != null)
                   {
                       Log.Information("*** Azure trace exporter enabled ***");
                       builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = config.Telemetry.AzureConnectionString);
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
          .AddSingleton(new RetryPolicy(config))
          .AddHttpClients(config)
          .AddKeycloakAuth(config)
          .AddSingleton<IClock>(SystemClock.Instance)
          .AddSingleton<Microsoft.Extensions.Logging.ILogger>(svc => svc.GetRequiredService<ILogger<IncomingUserChangeModificationHandler>>())
          .AddSingleton<Microsoft.Extensions.Logging.ILogger>(svc => svc.GetRequiredService<ILogger<UserProvisioningHandler>>());

        services.AddAuthorization(options =>
        {
            //options.AddPolicy("Administrator", policy => policy.Requirements.Add(new RealmAccessRoleRequirement("administrator")));
        });



        services.AddDbContext<EdtDataStoreDbContext>(options => options
            .UseSqlServer(config.ConnectionStrings.EdtDataStore, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.AddMediatR(typeof(Startup).Assembly);

        services.AddHealthChecks()
                .AddCheck("liveliness", () => HealthCheckResult.Healthy())
                .AddSqlServer(config.ConnectionStrings.EdtDataStore, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new KabobCaseParameterTransformer())))
             .AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<Startup>())
             .AddJsonOptions(options =>
             {
                 options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                 options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
             });
        services.AddHttpClient();

        services.AddSingleton<OtelMetrics>();


        //services.AddSingleton<ProblemDetailsFactory, UserManagerProblemDetailsFactory>();
        //services.AddHealthChecks();

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service API", Version = "v1" });
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
       // services.AddFluentValidationRulesToSwagger();

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        // Validate EF migrations on startup
        using (var serviceScope = services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<EdtDataStoreDbContext>();
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

        Log.Logger.Information("### EDT Service Configuration complete");



        //services.AddKafkaConsumer(config);

    }
    private EdtServiceConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new EdtServiceConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information("### EDT Service Version:{0} ###", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Logger.Debug("### Edt Service Configuration:{0} ###", System.Text.Json.JsonSerializer.Serialize(config));

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
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API"));

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
        app.UseMetricServer();
        app.UseHttpMetrics(options =>
        {
            // This will preserve only the first digit of the status code.
            // For example: 200, 201, 203 -> 2xx
            options.ReduceStatusCodeCardinality();
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapMetrics();
            endpoints.MapHealthChecks("/health/liveness").AllowAnonymous();
        });

    }
}
