namespace Pidp;

using System.Reflection;
using System.Text.Json;
using Common.Kafka;
using Common.Utils;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
using Pidp.Data;
using Pidp.Extensions;
using Pidp.Features;
using Pidp.Features.CourtLocations;
using Pidp.Features.CourtLocations.Jobs;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Features.Organization.UserTypeService;
using Pidp.Features.Parties;
using Pidp.Helpers.Middleware;
using Pidp.Infrastructure;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.HttpClients;
using Pidp.Infrastructure.Services;
using Pidp.Infrastructure.Telemetry;
using Prometheus;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl.AdoJobStore;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using static Pidp.Models.Lookups.CourtLocation;

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

        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var knownProxies = this.Configuration.GetSection("KnownProxies").Value;


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
        .AddHttpClients(config)
        .AddKeycloakAuth(config)
        .AddScoped<IEmailService, EmailService>()
        .AddScoped<IPidpAuthorizationService, PidpAuthorizationService>()

        .AddSingleton<IClock>(SystemClock.Instance)
        .AddScoped<Infrastructure.HttpClients.Jum.JumClient>();

        services.AddSingleton<ProblemDetailsFactory, JpidpProblemDetailsFactory>();

        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new KabobCaseParameterTransformer())))
            .AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<Startup>())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.WriteIndented = true;

            })
            .AddHybridModelBinder();

        services.AddDbContext<PidpDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.PidpDatabase, npg => npg.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.Scan(scan => scan
            .FromAssemblyOf<Startup>()
            .AddClasses(classes => classes.AssignableTo<IRequestHandler>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddScoped<IUserTypeService, UserTypeService>();
        services.AddScoped<IOrgUnitService, OrgUnitService>();
        services.AddScoped<ICourtAccessService, CourtAccessService>();
        services.AddScoped<IProfileUpdateService, ProfileUpdateServiceImpl>();

        services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));


        services.AddHealthChecks()
                .AddCheck("liveliness", () => HealthCheckResult.Healthy())
                .AddNpgSql(config.ConnectionStrings.PidpDatabase, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "PIdP Web API", Version = "v1", Description = "DIAM main webapi entrypoint - all UI communication goes through this WebApi service currently" });
            options.DescribeAllParametersInCamelCase();

            // Configure authentication for Swagger
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
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            // remove invalid schema characters
            options.CustomSchemaIds(x => x.FullName.Replace("+", "."));
        });



        services.AddFluentValidationRulesToSwagger();

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };


        // Validate EF migrations on startup
        using (var serviceScope = services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<PidpDbContext>();
            try
            {
                dbContext.Database.Migrate();
                this.LoadCourts(dbContext);

            }
            catch (Exception ex)
            {
                Log.Error($"Database migration failure {string.Join(",", ex.Message)}");
                throw;
            }
        }

        var permitIDIRDemsAccess = Environment.GetEnvironmentVariable("PERMIT_IDIR_DEMS_ACCESS");
        if (permitIDIRDemsAccess != null && permitIDIRDemsAccess.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger.Warning("*** PERMIT_IDIR_DEMS_ACCESS=true - access to DEMS services for IDIR users is enabled - this is intended for NON production use only ***");
        }

        var permitIDIRCaseMgmtAccess = Environment.GetEnvironmentVariable("PERMIT_IDIR_CASE_MANAGEMENT");
        if (permitIDIRCaseMgmtAccess != null && permitIDIRCaseMgmtAccess.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger.Warning("*** PERMIT_IDIR_CASE_MANAGEMENT=true - access to case management for IDIR users is enabled - this is intended for NON production use only ***");
        }
        var permitMismatchedVCCreds = Environment.GetEnvironmentVariable("PERMIT_MISMATCH_VC_CREDS");
        if (permitMismatchedVCCreds != null && permitMismatchedVCCreds.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger.Warning("*** PERMIT_MISMATCH_VC_CREDS=true - lawyers with non-matching creds will be permitted - law info only will be used - this is intended for NON production use only ***");
        }

        services.AddQuartz(q =>
        {
            Log.Information("Starting scheduler..");
            q.SchedulerId = "Court-Access-Core";
            q.SchedulerName = "DIAM Scheduler";

            q.UsePersistentStore(store =>
            {
                // Use for PostgresSQL database
                store.UsePostgres(pgOptions =>
                {
                    pgOptions.UseDriverDelegate<PostgreSQLDelegate>();
                    pgOptions.ConnectionString = config.ConnectionStrings.PidpDatabase;
                    pgOptions.TablePrefix = "quartz.qrtz_";
                });
                store.UseJsonSerializer();
            });

            // Create a "key" for the job
            var jobKey = new JobKey("Court access trigger");

            q.AddJob<CourtAccessScheduledJob>(opts => opts.WithIdentity(jobKey));

            Log.Information($"Scheduling CourtAccessScheduledJob with params [{config.CourtAccess.PollCron}]");

            // Create a trigger for the job
            q.AddTrigger(opts => opts
                .ForJob(jobKey) // link to the HelloWorldJob
                .WithIdentity("CourtAccess-trigger") // give the trigger a unique name
                .WithCronSchedule(config.CourtAccess.PollCron));


            q.AddJob<CourtAccessScheduledJob>(opts => opts.WithIdentity(jobKey));


        });


        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        //services.AddApiVersioning(options =>
        //{
        //    options.ReportApiVersions = true;
        //    options.AssumeDefaultVersionWhenUnspecified = true;
        //    options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        //}).AddApiExplorer();

    }

    public void LoadCourts(PidpDbContext context)
    {
        var generator = new CourtLocationDataGenerator();
        foreach (var location in generator.Generate())
        {
            var existingLocation = context.CourtLocations.Where(loc => loc.Code == location.Code).FirstOrDefault();
            if (existingLocation == null)
            {
                Log.Information($"Adding court location {location.Name}");
                context.CourtLocations.Add(location);
            }
            context.SaveChanges();
        }
    }

    private PidpConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new PidpConfiguration();

        this.Configuration.Bind(config);

        services.AddSingleton(config);

        Log.Logger.Information($"### DIAM Core Version:{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion} ###");

        Log.Logger.Information($"Assembly version: {new AppInfo().GetAssemblyVersion()}");
        if (Environment.GetEnvironmentVariable("JUSTIN_SKIP_USER_EMAIL_CHECK") is not null and "true")
        {
            Log.Logger.Warning("*** JUSTIN EMAIL VERIFICATION IS DISABLED ***");
        }


        return config;
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.yaml", "DIAM Web API"));
        }


        //app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseExceptionHandler(
            new ExceptionHandlerOptions()
            {
                AllowStatusCode404Response = true,
                ExceptionHandlingPath = "/error"
            }
            );// "/error");



        app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var userId = httpContext.User.GetUserId();
            if (!userId.Equals(Guid.Empty))
            {
                diagnosticContext.Set("User", userId);
            }
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
            endpoints.MapHealthChecks("/health/liveness", new HealthCheckOptions { AllowCachingResponses = false }).WithMetadata(new AllowAnonymousAttribute());

        });




    }
}
