namespace ApprovalFlow.Auth;

using common.Constants.Auth;
using Common.Extensions;
using DIAM.Common.Helpers.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


public static class AuthenticationSetup
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, ApprovalFlowConfiguration config)
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.BcscAuthentication, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.BCServicesCard));

            options.AddPolicy(Policies.ApprovalAuthorization, policy => policy.RequireAuthenticatedUser()
        .RequireAuthenticatedUser().RequireAssertion(context =>
        {
            var hasAdminRole = context.User.IsInRole(Roles.Admin);
            var hasApprovalRole = context.User.IsInRole(Roles.Approvals);
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider && (c.Value == ClaimValues.Idir));
            return (hasAdminRole || hasApprovalRole) && hasClaim;
        }));

            options.AddPolicy(Policies.IdirAuthentication, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(Claims.IdentityProvider, ClaimValues.Idir));

            //options.AddPolicy(Policies.VerifiedCredentialsProvider, policy => policy
            //    .RequireAuthenticatedUser()
            //    .RequireClaim(Claims.IdentityProvider, ClaimValues.VerifiedCredentials));

            options.AddPolicy(Policies.VerifiedCredentialsProvider, policy => policy
           .RequireAuthenticatedUser().RequireAssertion(context =>
           {
               var hasDutyRole = context.User.IsInRole(Roles.DutyCounsel);
               var hasDefenceRole = context.User.IsInRole(Roles.DefenceCounsel);
               var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider && (c.Value == ClaimValues.VerifiedCredentials || c.Value == ClaimValues.Idir));
               return (hasDutyRole || hasDefenceRole) && hasClaim;
           }));


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
                                                                    c.Value == ClaimValues.Bcps ||
                                                                    c.Value == ClaimValues.VerifiedCredentials));

                        return hasRole || hasClaim;
                    }));

            options.AddPolicy(Policies.AllDemsIdentityProvider, policy => policy
                  .RequireAuthenticatedUser().RequireAssertion(context =>
                  {
                      var hasSARole = context.User.IsInRole(Roles.SubmittingAgency);
                      var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                                 (c.Value == ClaimValues.BCServicesCard ||
                                                                  c.Value == ClaimValues.Idir ||
                                                                  c.Value == ClaimValues.Phsa ||
                                                                  c.Value == ClaimValues.Bcps ||
                                                                  c.Value == ClaimValues.VerifiedCredentials));
                      return hasSARole || hasClaim;
                  }));

            options.AddPolicy(Policies.AdminAuthentication, policy => policy
               .RequireAuthenticatedUser().RequireAssertion(context =>
                {
                    var hasRole = context.User.IsInRole(Roles.Admin);
                    var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                               (
                                                                c.Value == ClaimValues.Idir || c.Value == ClaimValues.Adfs ||
                                                                c.Value == ClaimValues.Bcps));
                    return hasRole || hasClaim;
                }));

            options.AddPolicy(Policies.SubAgencyIdentityProvider, policy => policy
                        .RequireAuthenticatedUser()
                        .RequireRole(Roles.SubmittingAgency));


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
