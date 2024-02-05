namespace edt.casemanagement.Infrastructure.Auth;

using System.Security.Claims;
using EdtService.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

public static class AuthenticationSetup
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, EdtServiceConfiguration config)
    {
        services.ThrowIfNull(nameof(services));
        config.ThrowIfNull(nameof(config));

        Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

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

            /////////////////////////////////
            // BC SERVICES CARD HOLDERS
            /////////////////////////////////
            options.AddPolicy(Policies.BcscAuthentication, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.BCServicesCard));

            /////////////////////////////////
            // BC IDIR USERS
            /////////////////////////////////
            options.AddPolicy(Policies.IdirAuthentication, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.Idir));


            /////////////////////////////////
            // BC PROSECUTION SERVICE
            /////////////////////////////////
            options.AddPolicy(Policies.BcpsAuthentication, policy => policy
                  .RequireAuthenticatedUser()
                  .RequireClaim(Claims.IdentityProvider, ClaimValues.Bcps));

            /////////////////////////////////
            // USERS WITH A PART ID
            /////////////////////////////////
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

            /////////////////////////////////
            // DEFENSE COUNSEL
            /////////////////////////////////
            options.AddPolicy(Policies.DefenceConselIdentityProvider, policy => policy
                .RequireAuthenticatedUser().RequireAssertion(context =>
                {
                    var hasRole = context.User.IsInRole(Roles.DefenceCounsel);
                    var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (c.Value == ClaimValues.VerifiedCredentials));
                    return hasRole || hasClaim;
                }));


            /////////////////////////////////
            // DUTY COUNSEL
            /////////////////////////////////
            options.AddPolicy(Policies.DutyConselIdentityProvider, policy => policy
                .RequireAuthenticatedUser().RequireAssertion(context =>
                {
                    var hasRole = context.User.IsInRole(Roles.DutyCounsel);
                    var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (c.Value == ClaimValues.VerifiedCredentials));
                    return hasRole || hasClaim;
                }));

            /////////////////////////////////
            // DUTY AND DEFENSE COUNSEL
            /////////////////////////////////
            options.AddPolicy(Policies.DutyConselIdentityProvider, policy => policy
                .RequireAuthenticatedUser().RequireAssertion(context =>
                {
                    var hasRole = context.User.IsInRole(Roles.DutyCounsel) || context.User.IsInRole(Roles.DefenceCounsel);
                    var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (c.Value == ClaimValues.VerifiedCredentials));
                    return hasRole || hasClaim;
                }));


            /////////////////////////////////
            // ANYONE WITH DEMS ACCESS
            /////////////////////////////////
            options.AddPolicy(Policies.AllDemsIdentityProvider, policy => policy
                  .RequireAuthenticatedUser().RequireAssertion(context =>
                  {
                      var hasRole = context.User.IsInRole(Roles.SubmittingAgency);
                      var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                                 (c.Value == ClaimValues.BCServicesCard ||
                                                                  c.Value == ClaimValues.Idir ||
                                                                  c.Value == ClaimValues.Phsa ||
                                                                  c.Value == ClaimValues.Bcps ||
                                                                  c.Value == ClaimValues.VerifiedCredentials));
                      return hasRole || hasClaim;
                  }));

            /////////////////////////////////
            // ADMIN USERS - GIVE WITH CAUTION!
            /////////////////////////////////
            options.AddPolicy(Policies.AdminAuthentication, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Claims.IdentityProvider, ClaimValues.Idir, ClaimValues.Bcps));


            /////////////////////////////////
            // SUBMITTING AGENCIES (POLICE)
            /////////////////////////////////
            options.AddPolicy(Policies.SubAgencyIdentityProvider, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.SubmittingAgency));

            /////////////////////////////////
            // USER OWNS RESOURCE
            /////////////////////////////////
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
