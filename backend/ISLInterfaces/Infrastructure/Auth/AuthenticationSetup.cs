namespace ISLInterfaces.Infrastructure.Auth;

using System.Security.Claims;
using Common.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

public static class AuthenticationSetup
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, IConfiguration config)
    {

        var keycloakOptions = new KeycloakConfiguration();
        config.GetSection(ISLInterfacesConfiguration.KeycloakConfig).Bind(keycloakOptions);

        Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        //        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakOptions.RealmUrl;
            options.RequireHttpsMetadata = false;
            options.Audience = Clients.PidpService;
            options.MetadataAddress = keycloakOptions.WellKnownConfig;
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context => await OnChallenge(context),
                OnForbidden = async context => await OnForbidden(context),
                OnAuthenticationFailed = async context => await OnAuthenticationFailure(context),
                OnTokenValidated = async context => await OnTokenValidatedAsync(context),

            };
        });

        // fallback policy
        services.AddAuthorizationBuilder().AddFallbackPolicy("fallback", policy => policy.RequireAuthenticatedUser());

        return services;
    }

    private static Task OnChallenge(JwtBearerChallengeContext context)
    {
        Serilog.Log.Debug($"Authentication challenge {context.Request.Path}");
        return Task.CompletedTask;
    }

    private static Task OnAuthenticationFailure(AuthenticationFailedContext context)
    {
        Serilog.Log.Warning($"Authentication failure {context.HttpContext.Request.Path} {context.Exception.Message}");
        return Task.CompletedTask;
    }


    private static Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is ClaimsIdentity identity
            && identity.IsAuthenticated)
        {
            // Flatten the Resource Access claim
            identity.AddClaims(identity.GetResourceAccessRoles(Clients.PidpService)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        return Task.CompletedTask;
    }


    private static Task OnForbidden(ForbiddenContext context)
    {
        Serilog.Log.Warning($"Authentication challenge {context.Request.Path}");
        return Task.CompletedTask;
    }
}
