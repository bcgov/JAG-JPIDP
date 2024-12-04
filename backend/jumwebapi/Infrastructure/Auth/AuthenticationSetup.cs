using System.Net;
using Common.Authorization;
using Common.Constants.Auth;
using Common.Kafka;
using Confluent.Kafka;
using jumwebapi.Extensions;
using jumwebapi.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace jumwebapi.Infrastructure.Auth
{
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, JumWebApiConfiguration config)
        {
            services.ThrowIfNull(nameof(services));
            config.ThrowIfNull(nameof(config));

            var clientConfig = new ClientConfig()
            {
                BootstrapServers = config.KafkaCluster.BootstrapServers,
                SaslMechanism = SaslMechanism.OAuthBearer,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslOauthbearerTokenEndpointUrl = config.KafkaCluster.SaslOauthbearerTokenEndpointUrl,
                SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
                SocketKeepaliveEnable = true,
                SaslOauthbearerScope = config.KafkaCluster.Scope,
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https,
                SslCaLocation = config.KafkaCluster.SslCaLocation,
                ConnectionsMaxIdleMs = 600000,
                SslCertificateLocation = config.KafkaCluster.SslCertificateLocation,
                SslKeyLocation = config.KafkaCluster.SslKeyLocation
            };


            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config.KafkaCluster.BootstrapServers,
                Acks = Acks.All,
                SaslMechanism = SaslMechanism.OAuthBearer,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslOauthbearerTokenEndpointUrl = config.KafkaCluster.SaslOauthbearerTokenEndpointUrl,
                SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
                SaslOauthbearerScope = config.KafkaCluster.Scope,
                SslEndpointIdentificationAlgorithm = (config.KafkaCluster.HostnameVerification == SslEndpointIdentificationAlgorithm.Https.ToString()) ? SslEndpointIdentificationAlgorithm.Https : SslEndpointIdentificationAlgorithm.None,
                SslCaLocation = config.KafkaCluster.SslCaLocation,
                SaslOauthbearerClientId = config.KafkaCluster.SaslOauthbearerProducerClientId,
                SaslOauthbearerClientSecret = config.KafkaCluster.SaslOauthbearerProducerClientSecret,
                SslCertificateLocation = config.KafkaCluster.SslCertificateLocation,
                SslKeyLocation = config.KafkaCluster.SslKeyLocation,
                EnableIdempotence = true,
                RetryBackoffMs = 1000,
                MessageSendMaxRetries = 3
            };

            services.AddSingleton(producerConfig);
            services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));



            var consumerConfig = new ConsumerConfig(clientConfig)
            {
                GroupId = config.KafkaCluster.ConsumerGroupId,
                EnableAutoCommit = true,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                ClientId = Dns.GetHostName(),
                EnableAutoOffsetStore = false,
                AutoCommitIntervalMs = 4000,
                BootstrapServers = config.KafkaCluster.BootstrapServers,
                SaslOauthbearerClientId = config.KafkaCluster.SaslOauthbearerConsumerClientId,
                SaslOauthbearerClientSecret = config.KafkaCluster.SaslOauthbearerConsumerClientSecret,
                SaslMechanism = SaslMechanism.OAuthBearer,
                SecurityProtocol = SecurityProtocol.SaslSsl
            };
            services.AddSingleton(consumerConfig);

            services.AddSingleton(producerConfig);
            services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));

            Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

            //JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = KeycloakUrls.Authority(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl);
                options.RequireHttpsMetadata = false;
                options.Audience = Resources.JumApi;
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
                                context.Response.Headers.Append("Token-Expired", "true");
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
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("Administrator", policy => policy.Requirements.Add(new RealmAccessRoleRequirement("administrator")));
            });
            return services;

        }
    }

}
