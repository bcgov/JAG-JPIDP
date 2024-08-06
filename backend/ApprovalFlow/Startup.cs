namespace ApprovalFlow;



using System.Reflection;
using System.Text.Json;
using ApprovalFlow.Auth;
using ApprovalFlow.Data;
using ApprovalFlow.Kafka;
using ApprovalFlow.Telemetry;
using Common.Constants.Telemetry;
using Common.Logging;
using DIAM.Common.Helpers.Transformers;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
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
using Serilog;
using Swashbuckle.AspNetCore.Filters;

public class Startup
{
    public IConfiguration Configuration { get; }
    private readonly string _policyName = "CorsPolicy";

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
                 serviceName: TelemetryConstants.ServiceName + "-ApprovalFlow",
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
                       .AddAspNetCoreInstrumentation());

        }

        services
          .AddAutoMapper(typeof(Startup))
          .AddKafkaConsumer(config)
          .AddKeycloakAuth(config)


          .AddSingleton<IClock>(SystemClock.Instance);
        //     .AddSingleton<Microsoft.Extensions.Logging.ILogger>(svc => svc.GetRequiredService<ILogger<CourtLocationAccessRequestHandler>>());

        services.AddDbContext<ApprovalFlowDataStoreDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.ApprovalFlowDataStore, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));

        services.AddHealthChecks()
                .AddCheck("liveliness", () => HealthCheckResult.Healthy())
                .AddNpgSql(config.ConnectionStrings.ApprovalFlowDataStore, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddCors(opt =>
        {
            opt.AddPolicy(name: this._policyName, builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });


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
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Approval Service API", Version = "v1", Description = "Requests and responses for Approvals required for DIAM" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
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
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApprovalFlowDataStoreDbContext>();
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


        Log.Logger.Information("### Approval Flow Configuration complete");


    }
    private ApprovalFlowConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new ApprovalFlowConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information("### Approval Flow Service Version:{0} ###", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Logger.Debug("### Approval Flow Configuration:{0} ###", System.Text.Json.JsonSerializer.Serialize(config));

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
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.yaml", "Approval Service API"));

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
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets(); // websocket support for auto-ui updates
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
