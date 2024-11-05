namespace Pidp.Infrastructure.Auth;

using System.Security.Claims;
using Common.Authorization;
using Common.Constants.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Pidp.Extensions;
using Serilog;

public static class AuthenticationSetup
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, PidpConfiguration config)
    {

        services.ThrowIfNull(nameof(services));
        config.ThrowIfNull(nameof(config));

        // fix for dotnet 8
        Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        //JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidAlgorithms = new List<string>() { "RS256" }

            };
            options.Authority = KeycloakUrls.Authority(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
            options.IncludeErrorDetails = true;
            options.RequireHttpsMetadata = true;
            options.Audience = Clients.PidpService;
            options.MetadataAddress = KeycloakUrls.WellKnownConfig(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context => await OnTokenValidatedAsync(context),
                OnChallenge = async context => await OnChallenge(context),
                OnForbidden = async context => await OnForbidden(context),
                OnAuthenticationFailed = async context => await OnAuthenticationFailure(context)
            };
        });
        services.AddScoped<IAuthorizationHandler, UserOwnsResourceHandler>();

        // BCPS logins
        services.AddAuthorizationBuilder().AddPolicy(Policies.BcpsAuthentication, policy => policy.RequireAuthenticatedUser()
                          .RequireClaim(Claims.IdentityProvider, ClaimValues.Bcps));

        // Submitting agency logins
        services.AddAuthorizationBuilder().AddPolicy(Policies.SubAgencyIdentityProvider, policy => policy.RequireAuthenticatedUser()
                                  .RequireRole(Roles.SubmittingAgency));

        // BC services card policy
        services.AddAuthorizationBuilder().AddPolicy(Policies.BcscAuthentication, policy => policy.RequireAuthenticatedUser().RequireClaim(Claims.IdentityProvider, ClaimValues.BCServicesCard));

        // access to approvals
        services.AddAuthorizationBuilder().AddPolicy(Policies.ApprovalAuthorization, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            {
                var hasAdminRole = context.User.IsInRole(Roles.Admin);
                var hasApprovalRole = context.User.IsInRole(Roles.Approver);
                var hasReadOnlyApprovalRole = context.User.IsInRole(Roles.ApprovalViewer);
                var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider && (c.Value == ClaimValues.Idir || c.Value == ClaimValues.Adfs));
                return (hasAdminRole || hasApprovalRole || hasReadOnlyApprovalRole) && hasClaim;
            }));


        // requires IDIR login
        services.AddAuthorizationBuilder().AddPolicy(Policies.IdirAuthentication, policy => policy.RequireAuthenticatedUser()
                        .RequireClaim(Claims.IdentityProvider, ClaimValues.Idir));

        // requires VC login (lawyers)
        services.AddAuthorizationBuilder().AddPolicy(Policies.VerifiedCredentialsProvider, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            {
                var hasDutyRole = context.User.IsInRole(Roles.DutyCounsel);
                var hasDefenceRole = context.User.IsInRole(Roles.DefenceCounsel);
                var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider && (c.Value == ClaimValues.VerifiedCredentials || c.Value == ClaimValues.Idir));
                return (hasDutyRole || hasDefenceRole) && hasClaim;
            }));


        // any DEMS possible user (should be more generic!)
        services.AddAuthorizationBuilder().AddPolicy(Policies.AllDemsIdentityProvider, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {
            var hasSARole = context.User.IsInRole(Roles.SubmittingAgency);
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (c.Value == ClaimValues.BCServicesCard ||
                                                        c.Value == ClaimValues.Idir ||
                                                        c.Value == ClaimValues.Bcps ||
                                                        c.Value == ClaimValues.VerifiedCredentials));
            return hasSARole || hasClaim;
        }));


        // any JAM_POR possible user (should be more generic!)
        services.AddAuthorizationBuilder().AddPolicy(Policies.AllJAMIdentityProvider, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {
            if (config.AllowUserPassTestAccounts && !context.User.HasClaim(c => c.Type == Claims.IdentityProvider)) // test accounts
            {
                var username = context.User.Claims.FirstOrDefault(c => c.Type == "preferred_username");
                Log.Logger.Warning("**** Allowing test accounts to pass through - NOT FOR PRODUCTION USE ****");

                if (username != null && username.Value.StartsWith("tst"))
                {
                    Log.Logger.Warning($"**** Permitting test user {username} - adding keycloak provider ****");
                    return true;
                }
            }
            var hasSARole = context.User.IsInRole(Roles.JAM_POR);
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider && (
                                                        c.Value == ClaimValues.AzureAd));
            return hasSARole || hasClaim;
        }));

        // any party requirement
        services.AddAuthorizationBuilder().AddPolicy(Policies.AnyPartyIdentityProvider, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {

            // no IDP linked - using a username and password combo from keycloak - not for prod use!!
            if (config.AllowUserPassTestAccounts && !context.User.HasClaim(c => c.Type == Claims.IdentityProvider)) // test accounts
            {
                var username = context.User.Claims.FirstOrDefault(c => c.Type == "preferred_username");
                Log.Logger.Warning("**** Allowing test accounts to pass through - NOT FOR PRODUCTION USE ****");

                if (username != null && username.Value.StartsWith("tst"))
                {
                    Log.Logger.Warning($"**** Permitting test user {username} - adding keycloak provider ****");
                    return true;
                }
            }
            var hasRole = context.User.IsInRole(Roles.SubmittingAgency);
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (c.Value == ClaimValues.BCServicesCard ||
                                                        c.Value == ClaimValues.Idir ||
                                                        c.Value == ClaimValues.Bcps ||
                                                        c.Value == ClaimValues.AzureAd ||
                                                        c.Value == ClaimValues.VerifiedCredentials));

            return hasRole || hasClaim;
        }));

        // admin users
        services.AddAuthorizationBuilder().AddPolicy(Policies.AdminAuthentication, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {
            var hasRole = context.User.IsInRole(Roles.Admin);
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (
                                                        c.Value == ClaimValues.Bcps));
            return hasRole && hasClaim;
        }));

        services.AddAuthorizationBuilder().AddPolicy(Policies.AdminClientAuthentication, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.AuthorizedParties && c.Value == Clients.DiamInternal);
            return hasClaim;
        }));

        services.AddAuthorizationBuilder().AddPolicy(Policies.DiamInternalAuthentication, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.AuthorizedParties && c.Value == Clients.DiamInternal);
            return hasClaim;
        }));

        services.AddAuthorizationBuilder().AddPolicy(Policies.UserOwnsResource, policy => policy.Requirements.Add(new UserOwnsResourceRequirement()));


        // fallback policy
        services.AddAuthorizationBuilder().AddFallbackPolicy("fallback", policy => policy.RequireAuthenticatedUser());





        return services;
    }

    private static Task OnForbidden(ForbiddenContext context)
    {
        Serilog.Log.Warning($"Authentication failure {context.Result.Failure}");
        return Task.CompletedTask;
    }

    private static Task OnChallenge(JwtBearerChallengeContext context)
    {
        Serilog.Log.Warning($"Authentication challenge {context.Error}");
        return Task.CompletedTask;
    }

    private static Task OnAuthenticationFailure(AuthenticationFailedContext context)
    {
        Serilog.Log.Warning($"Authentication failure {context.HttpContext.Request.Path}");
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
}
