namespace Pidp.Infrastructure.Auth;

using System.Security.Claims;
using common.Constants.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Pidp.Extensions;

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
            options.Authority = config.Keycloak.RealmUrl;
            options.IncludeErrorDetails = true;
            options.RequireHttpsMetadata = true;
            options.Audience = Clients.PidpService;
            options.MetadataAddress = config.Keycloak.WellKnownConfig;
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

        // any party requirement
        services.AddAuthorizationBuilder().AddPolicy(Policies.AnyPartyIdentityProvider, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        {
            var hasRole = context.User.IsInRole(Roles.SubmittingAgency);
            var hasClaim = context.User.HasClaim(c => c.Type == Claims.IdentityProvider &&
                                                       (c.Value == ClaimValues.BCServicesCard ||
                                                        c.Value == ClaimValues.Idir ||
                                                        c.Value == ClaimValues.Bcps ||
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

        services.AddAuthorizationBuilder().AddPolicy(Policies.UserOwnsResource, policy => policy.Requirements.Add(new UserOwnsResourceRequirement()));


        // fallback policy
        services.AddAuthorizationBuilder().AddFallbackPolicy("fallback", policy => policy.RequireAuthenticatedUser());





        return services;
    }

    private static Task OnForbidden(ForbiddenContext context)
    {
        Serilog.Log.Warning($"Authentication challenge");
        return Task.CompletedTask;
    }

    private static Task OnChallenge(JwtBearerChallengeContext context)
    {
        Serilog.Log.Warning($"Authentication challenge");
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
