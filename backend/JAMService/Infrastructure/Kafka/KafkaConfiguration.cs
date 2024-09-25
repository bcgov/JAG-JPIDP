namespace JAMService.Infrastructure.Kafka;

using System.Net;
using Common.Kafka;
using CommonModels.Models.JUSTIN;
using Confluent.Kafka;
using JAMService.Features.JAMProvisioning;
using JAMService.ServiceEvents.JAMProvisioning;
using NodaTime;

public static class KafkaConfiguration
{
    public static IServiceCollection AddKafkaClients(this IServiceCollection services, JAMServiceConfiguration config)
    {

        services.AddHttpClient<IAccessTokenClient, AccessTokenClient>();


        var clientConfig = new ClientConfig()
        {
            BootstrapServers = config.KafkaCluster.BootstrapServers,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslOauthbearerTokenEndpointUrl = config.KafkaCluster.SaslOauthbearerTokenEndpointUrl,
            SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
            SaslOauthbearerScope = config.KafkaCluster.Scope,
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https,
            SslCaLocation = config.KafkaCluster.SslCaLocation,
            SslCertificateLocation = config.KafkaCluster.SslCertificateLocation,
            SslKeyLocation = config.KafkaCluster.SslKeyLocation,
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
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https,
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
        services.AddScoped<IKafkaHandler<string, JAMProvisioningRequestModel>, IncomingJamProvisioningHandler>();
        services.AddScoped<IJAMProvisioningService, JAMProvisioningService>();
        services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));

        // start backgrund service
        services.AddHostedService<IncomingJamProvisioningConsumer>();

        return services;
    }
}

