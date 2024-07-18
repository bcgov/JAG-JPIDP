namespace DIAMCornetService.Infrastructure;

using Common.Authorization;
using Common.Constants.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

public static class AuthorizationConfiguration
{
    public static IServiceCollection ConfigureAuthorization(this IServiceCollection services, DIAMCornetServiceConfiguration config)
    {
        services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
             .AddJwtBearer(options =>
             {
                 options.Authority = KeycloakUrls.Authority(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
                 options.RequireHttpsMetadata = false;
                 options.Audience = "DIAM-INTERNAL";
                 options.MetadataAddress = KeycloakUrls.WellKnownConfig(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
                 options.TokenValidationParameters = new TokenValidationParameters()
                 {
                     ValidateIssuerSigningKey = true,
                     ValidateIssuer = false,
                     ValidateAudience = false,
                     ValidAlgorithms = new List<string>() { "RS256" }
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

        return services;
    }
}
