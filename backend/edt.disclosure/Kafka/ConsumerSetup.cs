namespace edt.disclosure.Kafka;

using System.Net;
using Common.Models;
using Common.Models.EDT;
using Confluent.Kafka;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.ServiceEvents;
using edt.disclosure.ServiceEvents.CourtLocation;
using edt.disclosure.ServiceEvents.CourtLocation.Handler;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.DefenceUserAccountCreation.Handler;
using edt.disclosure.ServiceEvents.UserAccountCreation;
using edt.disclosure.ServiceEvents.UserAccountCreation.Handler;
using edt.disclosure.ServiceEvents.UserAccountModification;
using EdtDisclosureService.Extensions;

public static class ConsumerSetup
{

    private static ProducerConfig? producerConfig;

    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services, EdtDisclosureServiceConfiguration config)
    {
        //Configuration = configuration;
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
            SslEndpointIdentificationAlgorithm = (config.KafkaCluster.HostnameVerification == SslEndpointIdentificationAlgorithm.Https.ToString()) ? SslEndpointIdentificationAlgorithm.Https : SslEndpointIdentificationAlgorithm.None,
            SslCaLocation = config.KafkaCluster.SslCaLocation,
            ConnectionsMaxIdleMs = 600000,
            SslCertificateLocation = config.KafkaCluster.SslCertificateLocation,
            SslKeyLocation = config.KafkaCluster.SslKeyLocation
        };
        producerConfig = new ProducerConfig()
        {
            BootstrapServers = config.KafkaCluster.BootstrapServers,
            Acks = Acks.All,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslOauthbearerTokenEndpointUrl = config.KafkaCluster.SaslOauthbearerTokenEndpointUrl,
            SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
            SaslOauthbearerScope = config.KafkaCluster.Scope,
            ClientId = Dns.GetHostName(),
            RequestTimeoutMs = 60000,
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

        services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));

        services.AddScoped<IKafkaHandler<string, CourtLocationDomainEvent>, CourtLocationAccessRequestHandler>();
        services.AddScoped<IKafkaHandler<string, EdtDisclosureDefenceUserProvisioningModel>, DefenceUserProvisioningHandler>();
        services.AddScoped<IKafkaHandler<string, EdtDisclosurePublicUserProvisioningModel>, PublicUserProvisioningHandler>();
        services.AddScoped<IKafkaHandler<string, UserChangeModel>, UserChangeHandler>();

        services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));

        services.AddHostedService<CourtLocationConsumer>();
        services.AddHostedService<DefenceUserProvisioningConsumer>();
        services.AddHostedService<PublicUserProvisioningConsumer>();
        services.AddHostedService<UserModificationConsumer>();

        return services;
    }

    public static ProducerConfig GetProducerConfig()
    {
        return producerConfig;
    }
}
