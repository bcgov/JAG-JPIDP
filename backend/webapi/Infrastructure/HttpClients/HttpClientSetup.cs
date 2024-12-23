namespace Pidp.Infrastructure.HttpClients;

using System.Net;
using Common.Authorization;
using Common.Constants.Auth;
using Common.Models.CORNET;
using CommonModels.Models.DIAMAdmin;
using CommonModels.Models.JUSTIN;
using Confluent.Kafka;
using IdentityModel.Client;
using Pidp.Extensions;
using Pidp.Infrastructure.HttpClients.AddressAutocomplete;
using Pidp.Infrastructure.HttpClients.Claims;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Infrastructure.HttpClients.Ldap;
using Pidp.Infrastructure.HttpClients.Mail;
using Pidp.Infrastructure.HttpClients.Plr;
using Pidp.Kafka.Consumer;
using Pidp.Kafka.Consumer.DIAMAdminRequests;
using Pidp.Kafka.Consumer.DomainEventResponses;
using Pidp.Kafka.Consumer.InCustodyProvisioning;
using Pidp.Kafka.Consumer.JustinParticipantMerges;
using Pidp.Kafka.Consumer.JustinUserChanges;
using Pidp.Kafka.Consumer.Notifications;
using Pidp.Kafka.Consumer.Responses;
using Pidp.Kafka.Interfaces;
using Pidp.Kafka.Producer;
using Pidp.Models;

public static class HttpClientSetup
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, PidpConfiguration config)
    {
        services.AddHttpClient<IAccessTokenClient, AccessTokenClient>();

        services.AddHttpClientWithBaseAddress<IAddressAutocompleteClient, AddressAutocompleteClient>(config.AddressAutocompleteClient.Url);

        services.AddHttpClientWithBaseAddress<IChesClient, ChesClient>(config.ChesClient.Url)
            .WithBearerToken(new ChesClientCredentials
            {
                Address = config.ChesClient.TokenUrl,
                ClientId = config.ChesClient.ClientId,
                ClientSecret = config.ChesClient.ClientSecret
            });

        services.AddHttpClientWithBaseAddress<ILdapClient, LdapClient>(config.LdapClient.Url);
        services.AddHttpClientWithBaseAddress<IEdtCaseManagementClient, EdtCaseManagementClient>(config.EdtCaseManagementClient.Url);
        services.AddHttpClientWithBaseAddress<IEdtDisclosureClient, EdtDisclosureClient>(config.EdtDisclosureClient.Url);
        services.AddHttpClientWithBaseAddress<IEdtCoreClient, EdtCoreClient>(config.EdtClient.Url).WithBearerToken(new InternalHttpRequestCredentials
        {
            Address = config.EdtClient.RealmUrl,
            ClientId = config.EdtClient.ClientId,
            ClientSecret = config.EdtClient.ClientSecret
        });

        services.AddHttpClientWithBaseAddress<IJUSTINClaimClient, JUSTINClaimClient>(config.JustinClaimClient.Url).WithBearerToken(new InternalJustinRequestCredentials
        {
            Address = config.JustinClaimClient.TokenUrl,
            ClientId = config.JustinClaimClient.ClientId,
            ClientSecret = config.JustinClaimClient.ClientSecret
        });

        services.AddHttpClientWithBaseAddress<IJumClient, JumClient>(config.JumClient.Url).WithBearerToken(new KeycloakAdministrationClientCredentials
        {
            Address = KeycloakUrls.Token(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl),
            ClientId = config.Keycloak.AdministrationClientId,
            ClientSecret = config.Keycloak.AdministrationClientSecret
        });


        services.AddHttpClientWithBaseAddress<IKeycloakAdministrationClient, KeycloakAdministrationClient>(config.Keycloak.AdministrationUrl)
            .WithBearerToken(new KeycloakAdministrationClientCredentials
            {
                Address = KeycloakUrls.Token(RealmConstants.BCPSRealm, config.Keycloak.RealmUrl),
                ClientId = config.Keycloak.AdministrationClientId,
                ClientSecret = config.Keycloak.AdministrationClientSecret
            });

        services.AddHttpClientWithBaseAddress<IPlrClient, PlrClient>(config.PlrClient.Url);

        services.AddTransient<ISmtpEmailClient, SmtpEmailClient>();

        var hostVerification = (config.KafkaCluster.HostnameVerification == SslEndpointIdentificationAlgorithm.Https.ToString()) ? SslEndpointIdentificationAlgorithm.Https : SslEndpointIdentificationAlgorithm.None;
        Serilog.Log.Information($"Host verification set to {hostVerification}");

        var clientConfig = new ClientConfig()
        {
            BootstrapServers = config.KafkaCluster.BootstrapServers,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslOauthbearerTokenEndpointUrl = config.KafkaCluster.SaslOauthbearerTokenEndpointUrl,
            SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
            SaslOauthbearerScope = config.KafkaCluster.Scope,
            SslEndpointIdentificationAlgorithm = hostVerification,
            SslCaLocation = config.KafkaCluster.SslCaLocation,
            SslCertificateLocation = config.KafkaCluster.SslCertificateLocation,
            SslKeyLocation = config.KafkaCluster.SslKeyLocation,
            ConnectionsMaxIdleMs = 2147483647
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
            ClientId = Dns.GetHostName(),
            LogConnectionClose = false,
            SslEndpointIdentificationAlgorithm = hostVerification,
            SslCaLocation = config.KafkaCluster.SslCaLocation,
            SaslOauthbearerClientId = config.KafkaCluster.SaslOauthbearerProducerClientId,
            SaslOauthbearerClientSecret = config.KafkaCluster.SaslOauthbearerProducerClientSecret,
            SslCertificateLocation = config.KafkaCluster.SslCertificateLocation,
            SslKeyLocation = config.KafkaCluster.SslKeyLocation,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3
        };

        var consumerConfig = new ConsumerConfig(clientConfig)
        {
            GroupId = config.KafkaCluster.ConsumerGroupId,
            EnableAutoCommit = true,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SaslOauthbearerClientId = config.KafkaCluster.SaslOauthbearerConsumerClientId,
            SaslOauthbearerClientSecret = config.KafkaCluster.SaslOauthbearerConsumerClientSecret,
            EnableAutoOffsetStore = false,
            ClientId = Dns.GetHostName(),
            SessionTimeoutMs = 60000,
            BootstrapServers = config.KafkaCluster.BootstrapServers,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            HeartbeatIntervalMs = 20000
        };

        services.AddSingleton(consumerConfig);
        services.AddSingleton(producerConfig);
        services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));

        services.AddScoped<IKafkaHandler<string, NotificationAckModel>, NotificationAckHandler>();
        services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));
        services.AddHostedService<NotificationAckService>();
        services.AddScoped<IKafkaHandler<string, JustinUserChangeEvent>, JustinUserChangeHandler>();
        services.AddScoped<IKafkaHandler<string, GenericProcessStatusResponse>, DomainEventResponseHandler>();
        services.AddScoped<IKafkaHandler<string, InCustodyParticipantModel>, InCustodyHandler>();
        services.AddScoped<IKafkaHandler<string, ParticipantMergeDetailModel>, JustinParticipantMergeHandler>();

        services.AddScoped<IKafkaHandler<AdminRequestKey, AdminRequestModel>, DIAMAdminRequestHandler>();

        services.AddHostedService<JustinUserChangeService>();
        services.AddHostedService<DomainEventResponseService>();
        services.AddHostedService<DIAMAdminMessageConsumer>();

        return services;
    }

    public static IHttpClientBuilder AddHttpClientWithBaseAddress<TClient, TImplementation>(this IServiceCollection services, string baseAddress)
        where TClient : class
        where TImplementation : class, TClient
        => services.AddHttpClient<TClient, TImplementation>(client => client.BaseAddress = new Uri(baseAddress.EnsureTrailingSlash()));

    public static IHttpClientBuilder WithBearerToken<T>(this IHttpClientBuilder builder, T credentials) where T : ClientCredentialsTokenRequest
    {
        builder.Services.AddSingleton(credentials)
            .AddScoped<BearerTokenHandler<T>>();

        builder.AddHttpMessageHandler<BearerTokenHandler<T>>();

        return builder;
    }

    public static IHttpClientBuilder WithDIAMBearerToken<T>(this IHttpClientBuilder builder, T credentials) where T : ClientCredentialsTokenRequest
    {
        builder.Services.AddSingleton(credentials)
            .AddScoped<BearerTokenHandler<T>>();

        builder.AddHttpMessageHandler<BearerTokenHandler<T>>();

        return builder;
    }
}
