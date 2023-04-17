namespace edt.casemanagement.Infrastructure.Auth;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using EdtService.Extensions;

public static class AuthenticationSetup
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, EdtServiceConfiguration config)
    {
        services.ThrowIfNull(nameof(services));
        config.ThrowIfNull(nameof(config));

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = config.Keycloak.RealmUrl;
            //options.Audience = Resources.PidpApi;
            options.RequireHttpsMetadata = false;
            options.Audience = Clients.PidpApi;
            options.MetadataAddress = config.Keycloak.WellKnownConfig;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context => await OnTokenValidatedAsync(context)
            };
        });
        services.AddScoped<IAuthorizationHandler, UserOwnsResourceHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.BcscAuthentication, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.BCServicesCard));

            options.AddPolicy(Policies.IdirAuthentication, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.Idir));

            options.AddPolicy(Policies.BcpsAuthentication, policy => policy
                  .RequireAuthenticatedUser()
                  .RequireClaim(Claims.IdentityProvider, ClaimValues.Bcps));

            options.AddPolicy(Policies.AnyPartyIdentityProvider, policy => policy
                  .RequireAuthenticatedUser().RequireAssertion(context =>
                  {
                      var hasRole = context.User.IsInRole(Roles.SubmittingAgency);
                      var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                                 (c.Value == ClaimValues.BCServicesCard ||
                                                                  c.Value == ClaimValues.Idir ||
                                                                  c.Value == ClaimValues.Phsa ||
                                                                  c.Value == ClaimValues.Bcps));
                      return hasRole || hasClaim;
                  }));
            ;

            options.AddPolicy(Policies.AllDemsIdentityProvider, policy => policy
                  .RequireAuthenticatedUser().RequireAssertion(context =>
                  {
                      var hasRole = context.User.IsInRole(Roles.SubmittingAgency);
                      var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                                 (c.Value == ClaimValues.BCServicesCard ||
                                                                  c.Value == ClaimValues.Idir ||
                                                                  c.Value == ClaimValues.Phsa ||
                                                                  c.Value == ClaimValues.Bcps));
                      return hasRole || hasClaim;
                  }));

            options.AddPolicy(Policies.AdminAuthentication, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Claims.IdentityProvider, ClaimValues.Idir, ClaimValues.Bcps));


            options.AddPolicy(Policies.SubAgencyIdentityProvider, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.SubmittingAgency));

            options.AddPolicy(Policies.UserOwnsResource, policy => policy.Requirements.Add(new UserOwnsResourceRequirement()));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.BCServicesCard, ClaimValues.Idir, ClaimValues.Phsa, ClaimValues.Bcps)
                .Build();
        });

        return services;
    }

    private static Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is ClaimsIdentity identity
            && identity.IsAuthenticated)
        {
            // Flatten the Resource Access claim
            identity.AddClaims(identity.GetResourceAccessRoles(Clients.PidpApi)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        return Task.CompletedTask;
    }
}
