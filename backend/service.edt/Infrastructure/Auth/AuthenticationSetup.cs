namespace edt.service.Infrastructure.Auth;

using System.Security.Claims;
using Common.Authorization;
using Common.Constants.Auth;
using edt.service;
using edt.service.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

public static class AuthenticationSetup
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, EdtServiceConfiguration config)
    {

        Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        //        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.UseSecurityTokenValidators = true;
            options.Authority = KeycloakUrls.Authority(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
            options.RequireHttpsMetadata = false;
            options.Audience = "DIAM-INTERNAL";
            options.MetadataAddress = KeycloakUrls.WellKnownConfig(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
            options.TokenValidationParameters = new TokenValidationParameters()
            {

                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = KeycloakUrls.Authority(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl),
                ValidateAudience = false,
                ValidAlgorithms = ["RS256"]
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    context.Response.OnStarting(async () =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var response =
                        JsonConvert.SerializeObject("The access token provided is not valid.");
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                            response =
                                JsonConvert.SerializeObject("The access token provided has expired.");
                        }
                        await context.Response.WriteAsync(response);
                    });

                    //context.HandleResponse();
                    //context.Response.WriteAsync(response).Wait();
                    return Task.CompletedTask;

                },
                OnForbidden = context =>
                {
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    if (string.IsNullOrEmpty(context.Error))
                        context.Error = "invalid_token";
                    if (string.IsNullOrEmpty(context.ErrorDescription))
                        context.ErrorDescription = "This request requires a valid JWT access token to be provided";

                    return context.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        error = context.Error,
                        error_description = context.ErrorDescription
                    }));
                }
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.DiamInternalAuthentication, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            {

                var hasClaim = context.User.HasClaim(c => c.Type == Claims.AuthorizedParties && c.Value == Clients.DiamInternal);
                return hasClaim;
            }));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy("Administrator", policy => policy.Requirements.Add(new RealmAccessRoleRequirement("administrator")));
        });
        return services;
    }

    private static Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is ClaimsIdentity identity
            && identity.IsAuthenticated)
        {
            // Flatten the Resource Access claim
            identity.AddClaims(identity.GetResourceAccessRoles(Clients.DiamInternal)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        return Task.CompletedTask;
    }
}
