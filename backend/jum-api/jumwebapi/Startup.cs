using FluentValidation.AspNetCore;
using jumwebapi.Data;
using jumwebapi.Infrastructure;
using jumwebapi.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using jumwebapi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using jumwebapi.Core.Http;
using MediatR;
using jumwebapi.Features.Players;
using jumwebapi.PipelineBehaviours;
using MediatR.Extensions.FluentValidation.AspNetCore;
using jumwebapi.Features.Participants.Services;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using jumwebapi.Common;
using jumwebapi.Features.DigitalParticipants.Services;
using jumwebapi.Features.Persons.Services;
using jumwebapi.Features.Agencies.Services;
using jumwebapi.Features.Users.Services;
using jumwebapi.Infrastructure.HttpClients;
using jumwebapi.Data.Seed;
using jumwebapi.Helpers.Mapping;

namespace jumwebapi;
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

        services.AddScoped<IPlayersService, PlayersService>();
        services.AddScoped<IPartyTypeService, PartyTypeService>();
        services.AddScoped<IDigitalParticipantService, DigitalParticipantService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IAgencyService, AgencyService>();
        services.AddScoped<IUserService, UserService>();

        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new KabobCaseParameterTransformer())))
            .AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<Startup>())
            .AddJsonOptions(options => options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))
            .AddHybridModelBinder();
        services.AddHttpClient();

        services.AddDbContext<JumDbContext>(options => options
            .UseSqlServer(config.ConnectionStrings.JumDatabase, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        services.AddMediatR(typeof(Startup).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        //services.AddFluentValidation(new[] { typeof(UpdatePlayerCommandHandler).GetTypeInfo().Assembly });
        //services.AddValidatorsFromAssembly(typeof(Startup).Assembly);
        //services.AddTransient<ExceptionHandlingMiddleware>();
        services.AddSingleton<ProblemDetailsFactory, UserManagerProblemDetailsFactory>();

        services.AddSingleton<IAuthorizationHandler, RealmAccessRoleHandler>();
        services.AddTransient<IClaimsTransformation, KeycloakClaimTransformer>();
        services.AddHttpContextAccessor();
        services.AddTransient<ClaimsPrincipal>(s => s.GetService<IHttpContextAccessor>().HttpContext.User);
        services.AddScoped<IProxyRequestClient, ProxyRequestClient>();
        services.AddScoped<IdentityProviderDataSeeder>();
        //services.AddScoped<IOpenIdConnectRequestClient, OpenIdConnectRequestClient>();


        services.AddHealthChecks()
            .AddCheck("liveliness", () => HealthCheckResult.Healthy());
            //.AddSqlServer(config.ConnectionStrings.JumDatabase, tags: new[] { "services" });

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "JUM Web API", Version = "v1" });
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
        services.AddFluentValidationRulesToSwagger();

    }
    private jumwebapiConfiguration InitializeConfiguration(IServiceCollection services)
    {
        var config = new jumwebapiConfiguration();
        this.Configuration.Bind(config);
        services.AddSingleton(config);

        Log.Logger.Information("### App Version:{0} ###", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Logger.Information("### JUM Configuration:{0} ###", JsonSerializer.Serialize(config));

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
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "JUM Web API"));

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            // endpoints.MapHealthChecks("/health");
        });

    }
}
