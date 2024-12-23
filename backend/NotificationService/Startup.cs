namespace NotificationService;

using System.Reflection;
using Common.Logging;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using NodaTime;
using NotificationService.Data;
using NotificationService.HttpClients;
using NotificationService.Kafka;
using NotificationService.Services;
using Prometheus;
using Serilog;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => this.Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        var config = this.InitializeConfiguration(services);
        services
          .AddAutoMapper(typeof(Startup))
          .AddKafkaConsumer(config)
          .AddHttpClients(config)
          .AddScoped<IEmailTemplateCache, LocalEMailTemplateCache>()
          .AddScoped<IEmailService, EmailService>()

          .AddSingleton<IClock>(SystemClock.Instance);

        services.AddAuthorization(options =>
        {
            //options.AddPolicy("Administrator", policy => policy.Requirements.Add(new RealmAccessRoleRequirement("administrator")));
        });

        //services.AddDbContext<NotificationDbContext>(options => options
        //    .UseSqlServer(config.ConnectionStrings.JumDatabase, sql => sql.UseNodaTime())
        //    .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));


        services.AddDbContext<NotificationDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.NotificationDatabase, npg => npg.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        //services.AddHealthChecks()
        //        .AddCheck("liveliness", () => HealthCheckResult.Healthy())
        //        .AddSqlServer(config.ConnectionStrings.JumDatabase, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddHealthChecks()
        .AddCheck("liveliness", () => HealthCheckResult.Healthy())
        .AddNpgSql(config.ConnectionStrings.NotificationDatabase, tags: new[] { "services" }).ForwardToPrometheus();

        services.AddControllers();
        services.AddHttpClient();

        //services.AddSingleton<ProblemDetailsFactory, UserManagerProblemDetailsFactory>();
        //services.AddHealthChecks();

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
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
            //options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.CustomSchemaIds(x => x.FullName);
        });
        services.AddFluentValidationRulesToSwagger();

        // Validate EF migrations on startup
        using (var serviceScope = services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<NotificationDbContext>();
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

        //services.AddKafkaConsumer(config);

    }
    private NotificationServiceConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new NotificationServiceConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);
        var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        Log.Logger.Information($"### Notification Service Version: {version} ###");


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


        app.UseMetricServer();
        app.UseHttpMetrics();

        app.UseRouting();
        app.UseCors("CorsPolicy");
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
