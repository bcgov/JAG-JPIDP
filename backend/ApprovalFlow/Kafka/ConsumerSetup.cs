namespace ApprovalFlow.Kafka;

using System.Net;
using ApprovalFlow.ServiceEvents.IncomingApproval;
using Common.Kafka;
using Common.Models.Approval;
using Confluent.Kafka;
using DIAM.Common.Helpers.Extensions;

public static class ConsumerSetup
{

    private static ProducerConfig? producerConfig;

    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services, ApprovalFlowConfiguration config)
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
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https,
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
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https,
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


        services.AddScoped<IKafkaHandler<string, ApprovalRequestModel>, IncomingApprovalHandler>();

        services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));

        services.AddHostedService<ApprovalConsumer>();

        return services;
    }

    public static ProducerConfig GetProducerConfig() => producerConfig;
}
