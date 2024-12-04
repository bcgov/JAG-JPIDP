namespace jumwebapi;

using System.Reflection;
using System.Security.Claims;
using FluentValidation.AspNetCore;
using global::Common.Kafka;
using global::Common.Logging;
using jumwebapi.Common;
using jumwebapi.Core.Http;
using jumwebapi.Data;
using jumwebapi.Data.Seed;
using jumwebapi.Extensions;

using jumwebapi.Features.ORDSTest;
using jumwebapi.Features.UserChangeManagement.Services;
using jumwebapi.Helpers.Mapping;
using jumwebapi.Infrastructure;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Infrastructure.HttpClients;
using jumwebapi.Kafka.Consumers.ParticipantMergeConsumer;
using jumwebapi.Models;
using jumwebapi.PipelineBehaviours;
using Mapster;
using MediatR;
using MediatR.Extensions.FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Prometheus;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl.AdoJobStore;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => this.Configuration = configuration;
    public void ConfigureServices(IServiceCollection services)
    {
        var config = this.InitializeConfiguration(services);
        // var jsonSerializerOptions = this.Configuration.GenerateJsonSerializerOptions();
        services
          .AddAutoMapper(typeof(Startup))
          .AddHttpClients(config)
          .AddKeycloakAuth(config)
          .AddSingleton<IClock>(NodaTime.SystemClock.Instance);

        services.AddMapster(options =>
        {
            options.Default.IgnoreNonMapped(true);
            options.Default.IgnoreNullValues(true);
            options.AllowImplicitDestinationInheritance = true;
            options.AllowImplicitSourceInheritance = true;
            options.Default.UseDestinationValue(member =>
                member.SetterModifier == AccessModifier.None &&
                member.Type.IsGenericType &&
                member.Type.GetGenericTypeDefinition() == typeof(ICollection<>));
        });


        services.AddControllers().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Administrator", policy => policy.Requirements.Add(new RealmAccessRoleRequirement("administrator")));
        });



        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new KabobCaseParameterTransformer())))
            .AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<Startup>())
            .AddJsonOptions(options => options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))
            .AddHybridModelBinder();
        services.AddHttpClient();

        services.AddDbContext<JumDbContext>(options => options
         .UseNpgsql(config.ConnectionStrings.JumDatabase, npg => npg.UseNodaTime())
         .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddSingleton<ProblemDetailsFactory, UserManagerProblemDetailsFactory>();


        services.AddSingleton<IAuthorizationHandler, RealmAccessRoleHandler>();
        services.AddTransient<IClaimsTransformation, KeycloakClaimTransformer>();
        services.AddHttpContextAccessor();
        services.AddTransient<ClaimsPrincipal>(s => s.GetService<IHttpContextAccessor>().HttpContext.User);
        services.AddScoped<IProxyRequestClient, ProxyRequestClient>();
        services.AddScoped<IdentityProviderDataSeeder>();

        // add consumer background service
        services.AddHostedService<ParticipantMergeConsumerService>();

        // add handler to process part merge messages
        services.AddScoped<IKafkaHandler<string, ParticipantMergeEvent>, ParticipantMergeConsumerHandler>();


        services.AddHealthChecks()
            .AddCheck("liveliness", () => HealthCheckResult.Healthy())
            .AddNpgSql(config.ConnectionStrings.JumDatabase, tags: new[] { "services" }).ForwardToPrometheus();

        //services.AddHealthChecks()
        //    .AddCheck("liveliness", () => HealthCheckResult.Healthy());
        //  //  .AddSqlServer(config.ConnectionStrings.JumDatabase, tags: new[] { "services" });

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "JUM Web API", Version = "v1", Description = "Provides APIs for JUSTIN info relating to DIAM" });
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
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Scheme = "bearer",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT"
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
        services.AddFluentValidationRulesToSwagger();

        // Validate EF migrations on startup
        using (var serviceScope = services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<JumDbContext>();
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

        // register background service for checking user changes in JUSTIN
        services.AddHostedService<UserChangeBackgroundService>();
        services.AddScoped<JustinUserChangeService>();

        Log.Logger.Information("### JUM Service Configuration complete");

        // test serivces for ORDS
        var testOrds = Environment.GetEnvironmentVariable("TEST_ORDS");
        if (!string.IsNullOrEmpty(testOrds) && testOrds.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger.Information("Starting ORDS tests...");
            services.AddQuartz(q =>
            {
                Log.Information("Starting ORDS Test scheduler..");
                var schedulerId = $"DIAM-JUM-Scehduler-{Guid.NewGuid()}";

                q.SchedulerId = schedulerId;
                q.SchedulerName = "ORDS Test Scheduler";

                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 3;
                });

                q.UsePersistentStore(store =>
                {
                    store.UseClustering(c =>
                    {
                        c.CheckinMisfireThreshold = TimeSpan.FromSeconds(15);
                        c.CheckinInterval = TimeSpan.FromSeconds(10);
                    });
                    // Use for PostgresSQL database
                    store.UsePostgres(pgOptions =>
                    {
                        pgOptions.UseDriverDelegate<PostgreSQLDelegate>();
                        pgOptions.ConnectionString = config.ConnectionStrings.JumDatabase;
                        pgOptions.TablePrefix = "quartz.qrtz_";
                    });
                    store.UseJsonSerializer();
                });

                // q.UseMicrosoftDependencyInjectionJobFactory();
                //q.UseSimpleTypeLoader();
                //q.UseInMemoryStore();
                //q.UseDefaultThreadPool(tp =>
                //{
                //    tp.MaxConcurrency = 2;
                //});
                var jobKey = new JobKey("JUM Test ORDS");
                q.AddJob<ORDSTestJob>(opts => opts.WithIdentity(jobKey));

                Log.Information($"Scheduling JUM Test ORDS with params [{config.TestORDSConfiguration.PollCron}]");

                // Create a trigger for the job
                q.AddTrigger(opts => opts
                    .ForJob(jobKey) // link to the HelloWorldJob
                    .WithIdentity("JUM Test ORDS trigger") // give the trigger a unique name
                    .WithDescription("JUM Test scheduled event for ORDS")
                    .WithCronSchedule(config.TestORDSConfiguration.PollCron));


                //q.ScheduleJob<ORDSTestJob>(trigger => trigger
                //  .WithIdentity("JUM Test ORDS trigger")
                //  .StartNow()
                //  .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
                //  .WithDescription("JUM Test scheduled event")
            });

            services.AddQuartzServer(options =>
            {
                options.WaitForJobsToComplete = true;
            });
        }





    }
    private JumWebApiConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new JumWebApiConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information($"### JUM API Version:{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion} ###");

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
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.yaml", "JUM Web API");
            options.EnablePersistAuthorization();
        }
        );

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
        app.UseHttpMetrics();
        app.UseMiddleware<CorrelationIdMiddleware>();

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
