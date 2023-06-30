namespace Pidp;

using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text.Json;

using Pidp.Data;
using Pidp.Extensions;
using Pidp.Features;
using Pidp.Infrastructure;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.HttpClients;
using Pidp.Infrastructure.Services;
using Pidp.Helpers.Middleware;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Pidp.Features.Organization.UserTypeService;
using Pidp.Features.Organization.OrgUnitService;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using Pidp.Infrastructure.Telemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Prometheus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Core;
using Pidp.Features.CourtLocations;
using Quartz;
using Quartz.Impl;
using static Quartz.Logging.OperationName;
using Pidp.Features.CourtLocations.Jobs;
using Pidp.Models.Lookups;
using static Pidp.Models.Lookups.CourtLocation;
using Microsoft.Extensions.DependencyInjection;

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

        var assemblyVersion = Assembly.GetExecutingAssembly()    .GetName().Version?.ToString() ?? "0.0.0";
        var knownProxies = Configuration.GetSection("KnownProxies").Value;


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
                    if (config.Telemetry.AzureConnectionString != null)
                    {
                        builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = config.Telemetry.AzureConnectionString);
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



        services.AddHealthChecks()
                .AddCheck("liveliness", () => HealthCheckResult.Healthy())
                .AddNpgSql(config.ConnectionStrings.PidpDatabase, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "PIdP Web API", Version = "v1" });
            options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.CustomSchemaIds(x => x.FullName);
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
                LoadCourts(dbContext);

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


        services.AddQuartz(q =>
        {
            Log.Information("Starting scheduler..");
            q.SchedulerId = "Court-Access-Core";
            q.SchedulerName = "DIAM Scheduler";
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 5;
            });

            q.ScheduleJob<CourtAccessScheduledJob>(trigger => trigger
              .WithIdentity("Court access trigger")
              .StartNow()
              .WithDailyTimeIntervalSchedule(x => x.WithInterval(config.CourtAccess.PollSeconds, IntervalUnit.Second))
              .WithDescription("Court access scheduled event")
          );
        });


        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });


        Log.Logger.Information("Startup configuration complete");



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

        Log.Logger.Information("### App Version:{0} ###", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Logger.Debug("### PIdP Configuration:{0} ###", System.Text.Json.JsonSerializer.Serialize(config));


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
        }


        //app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseExceptionHandler(
            new ExceptionHandlerOptions()
            {
                AllowStatusCode404Response = true,
                ExceptionHandlingPath = "/error"
            }
            );// "/error");

        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "PIdP Web API"));

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
            endpoints.MapHealthChecks("/health/liveness").AllowAnonymous();
        });



    }
}
