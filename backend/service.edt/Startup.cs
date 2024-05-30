
namespace edt.service;

using System.Reflection;
using System.Text.Json;
using edt.service.Data;
using edt.service.HttpClients;
using edt.service.Infrastructure.Auth;
using edt.service.Infrastructure.Telemetry;
using edt.service.Kafka;
using edt.service.ServiceEvents.PersonFolioLinkageHandler;
using edt.service.ServiceEvents.UserAccountCreation.ConsumerRetry;
using edt.service.ServiceEvents.UserAccountCreation.Handler;
using edt.service.ServiceEvents.UserAccountModification.Handler;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl.AdoJobStore;
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


        var workAroundTopic = Environment.GetEnvironmentVariable("USER_CREATION_PLAINTEXT_TOPIC");
        if (!string.IsNullOrEmpty(workAroundTopic))
        {
            Log.Information("*** Plain text user creation topic is configured {0} ***", workAroundTopic);
        }

        if (!string.IsNullOrEmpty(config.Telemetry.CollectorUrl))
        {

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
                {
                    builder
                     .AddMeter(Instrumentation.MeterName)
                     .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
                });

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
            //   options.AddPolicy("Administrator", policy => policy.Requirements.Add(new RealmAccessRoleRequirement("administrator")));
        });

        services.AddDbContext<EdtDataStoreDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.EdtDataStore, npg => npg.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));

        services.AddHealthChecks()
        .AddCheck("liveliness", () => HealthCheckResult.Healthy())
        .AddNpgSql(config.ConnectionStrings.EdtDataStore, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new KabobCaseParameterTransformer())))
             .AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<Startup>())
             .AddJsonOptions(options =>
             {
                 options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                 options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
             });
        services.AddHttpClient();

        services.AddSingleton<Instrumentation>();
        services.AddSingleton<IAuthorizationHandler, RealmAccessRoleHandler>();
        services.AddScoped<IFolioLinkageService, FolioLinkageService>();


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
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "EDT Core Service API", Version = "v1", Description = "DIAM APIs for accessing EDT related services" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("https://dev.common-sso.justice.gov.bc.ca/auth/realms/BCPS/protocol/openid-connect/auth"),
                        TokenUrl = new Uri("https://dev.common-sso.justice.gov.bc.ca/auth/realms/BCPS/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string>
                    {
                        { "openid" , "DIAM Server HTTP Api" }
                    },
                    }
                },
                Description = "DIAM Server OpenId Security Scheme"
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



        services.AddQuartz(q =>
        {
            Log.Information("Starting scheduler..");
            q.SchedulerId = "Folio-Linkage-Scheduler";
            q.SchedulerName = "DIAM Scheduler";

            q.UsePersistentStore(store =>
            {
                // Use for PostgresSQL database
                store.UsePostgres(pgOptions =>
                {
                    pgOptions.UseDriverDelegate<PostgreSQLDelegate>();
                    pgOptions.ConnectionString = config.ConnectionStrings.EdtDataStore;
                    pgOptions.TablePrefix = "quartz.qrtz_";
                });
                store.UseJsonSerializer();
            });


            var jobKey = new JobKey("Folio Linkage Jon");

            q.AddJob<FolioLinkageJob>(opts => opts.WithIdentity(jobKey));
            Log.Information($"Scheduling FolioLinkageBackgroundService with params [{config.FolioLinkageBackgroundService.PollCron}]");

            q.AddTrigger(opts => opts
                .ForJob(jobKey) // link to the HelloWorldJob
                .WithIdentity("Folio-linkage-trigger") // give the trigger a unique name
                .WithDescription("Court access scheduled event")
                .WithCronSchedule(config.FolioLinkageBackgroundService.PollCron));




        });


        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        Log.Logger.Information("### EDT Service Configuration complete");



        //services.AddKafkaConsumer(config);

    }
    private EdtServiceConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new EdtServiceConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information($"### EDT Service Version:{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion} ###");

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

        app.UseCors("CorsPolicy");
        app.UseMetricServer();
        app.UseHttpMetrics(options =>
        {
            // This will preserve only the first digit of the status code.
            // For example: 200, 201, 203 -> 2xx
            options.ReduceStatusCodeCardinality();
        });
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapMetrics();
            endpoints.MapHealthChecks("/health/liveness").AllowAnonymous();
        });


    }
}
