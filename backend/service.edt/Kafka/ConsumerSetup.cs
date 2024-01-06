namespace edt.service.Kafka;

using System.Net;
using Common.Models.EDT;
using Confluent.Kafka;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka.Interfaces;
using edt.service.ServiceEvents;
using edt.service.ServiceEvents.DefenceParticipantCreation;
using edt.service.ServiceEvents.PersonFolioLinkageHandler;
using edt.service.ServiceEvents.UserAccountCreation;
using edt.service.ServiceEvents.UserAccountCreation.ConsumerRetry;
using edt.service.ServiceEvents.UserAccountCreation.Handler;
using edt.service.ServiceEvents.UserAccountModification;
using edt.service.ServiceEvents.UserAccountModification.Handler;
using edt.service.ServiceEvents.UserAccountModification.Models;
using EdtService.Extensions;

public static class ConsumerSetup
{

    private static ProducerConfig? producerConfig;

    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services, EdtServiceConfiguration config)
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
            ConnectionsMaxIdleMs = 2147483647,
            TopicMetadataRefreshIntervalMs = 10000,
            SaslOauthbearerScope = config.KafkaCluster.Scope,
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https,
            SslCaLocation = config.KafkaCluster.SslCaLocation,
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

        // consumers need to have timeouts set to match retry events
        var retrySeconds = 0;
        config.RetryPolicy.RetryTopics.ForEach(retryTopic =>
        {
            retrySeconds += retryTopic.RetryCount * retryTopic.DelayMinutes * 60;
        });

        // give an extra minute
        retrySeconds += 60;
        Serilog.Log.Information("Consumer max wait is set to {0} seconds", retrySeconds);

        var consumerConfig = new ConsumerConfig(clientConfig)
        {
            GroupId = config.KafkaCluster.ConsumerGroupId,
            EnableAutoCommit = true,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            ClientId = Dns.GetHostName(),
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 60000,
            BootstrapServers = config.KafkaCluster.BootstrapServers,
            SaslOauthbearerClientId = config.KafkaCluster.SaslOauthbearerConsumerClientId,
            SaslOauthbearerClientSecret = config.KafkaCluster.SaslOauthbearerConsumerClientSecret,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SecurityProtocol = SecurityProtocol.SaslSsl
        };
        services.AddSingleton(consumerConfig);
        services.AddSingleton(producerConfig);

        services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));
        services.AddScoped<IKafkaHandler<string, IncomingUserModification>, IncomingUserChangeModificationHandler>();

        services.AddScoped<IKafkaHandler<string, EdtUserProvisioningModel>, UserProvisioningHandler>();
        services.AddScoped<IKafkaHandler<string, EdtPersonProvisioningModel>, DefenceParticipantHandler>();
        services.AddScoped<IKafkaHandler<string, PersonFolioLinkageModel>, EdtPersonFolioLinkageHandler>();

        services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));

        services.AddHostedService<EdtServiceConsumer>();
        services.AddHostedService<EdtPersonCreationConsumer>();
        services.AddHostedService<EdtPersonFolioLinkageConsumer>();
        services.AddHostedService<EdtUserModificationServiceConsumer>();

        services.AddHostedService<ConsumerRetryService>();
        return services;
    }

    public static ProducerConfig GetProducerConfig()
    {
        return producerConfig;
    }
}
