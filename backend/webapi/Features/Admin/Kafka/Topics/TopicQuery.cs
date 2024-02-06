namespace Pidp.Features.Admin.Kafka.Topics;

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Pidp.Features.Admin.Kafka.Models;

public record TopicQuery(string? topicName) : IQuery<List<TopicModel>>;

public class TopicQueryHandler : IQueryHandler<TopicQuery, List<TopicModel>>
{
    private PidpConfiguration configuration;

    public TopicQueryHandler(PidpConfiguration configuration)
    {
        this.configuration = configuration;

    }
    public async Task<List<TopicModel>> HandleAsync(TopicQuery query)
    {
        var topicData = new List<TopicModel>();

        if (string.IsNullOrEmpty(this.configuration.KafkaCluster.KafkaAdminClientId) || string.IsNullOrEmpty(this.configuration.KafkaCluster.KafkaAdminClientSecret))
        {
            Serilog.Log.Error("Kafka admin client id or secret is null");
            throw new Exception("Kafka admin client id or secret is null");
        }

        using (var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = this.configuration.KafkaCluster.BootstrapServers,
            SaslOauthbearerClientId = this.configuration.KafkaCluster.KafkaAdminClientId,
            SaslOauthbearerClientSecret = this.configuration.KafkaCluster.KafkaAdminClientSecret,
            SaslOauthbearerTokenEndpointUrl = this.configuration.KafkaCluster.SaslOauthbearerTokenEndpointUrl,
            SslCertificateLocation = this.configuration.KafkaCluster.SslCertificateLocation,
            SslCaLocation = this.configuration.KafkaCluster.SslCaLocation,
            SaslOauthbearerScope = this.configuration.KafkaCluster.Scope,
            SslEndpointIdentificationAlgorithm = (this.configuration.KafkaCluster.HostnameVerification == SslEndpointIdentificationAlgorithm.Https.ToString()) ? SslEndpointIdentificationAlgorithm.Https : SslEndpointIdentificationAlgorithm.None,
            SslKeyLocation = this.configuration.KafkaCluster.SslKeyLocation,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
            SecurityProtocol = SecurityProtocol.SaslSsl,
        }).Build())
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            var consumers = adminClient.ListGroups(TimeSpan.FromSeconds(5));


            Serilog.Log.Information($"Got topic information {metadata.Topics.Count}");

            foreach (var consumer in consumers)
            {
                Serilog.Log.Information($"Consumer {consumer.State} {consumer.Group} {consumer.Members.Count}");
            }

            foreach (var topic in metadata.Topics)
            {
                Serilog.Log.Information($"Topic {topic.Topic}");


                var totalCount = 0;

                topicData.Add(new TopicModel
                {
                    Name = topic.Topic,
                    Partitions = topic.Partitions.Count,
                    Consumers = await this.GetTopicConsumers(adminClient, topic.Topic),
                });
            }

        }
        return topicData;
    }

    private async Task<List<string>> GetTopicConsumers(IAdminClient adminClient, string topicName)
    {
        Serilog.Log.Information($"Getting consumers for {topicName}");

        var response = new List<string>();

        var groups = await adminClient.ListConsumerGroupsAsync(new ListConsumerGroupsOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(5),
        });


        var groupIds = groups.Valid.Select(group => group.GroupId).ToList();


        var groupDescriptions = await adminClient.DescribeConsumerGroupsAsync(groupIds);

        foreach (var groupInfo in groupDescriptions.ConsumerGroupDescriptions)
        {
            foreach (var member in groupInfo.Members)
            {
                Serilog.Log.Information($"Member {member.Assignment.TopicPartitions} {member.ConsumerId} {member.Assignment}");
                var forTopic = member.Assignment.TopicPartitions.Any(part => part.Topic == topicName);
                if (forTopic)
                {
                    response.Add(groupInfo.GroupId);
                }
            }
        }

        foreach (var group in groups.Valid)
        {
            Serilog.Log.Information($"Group {group.GroupId} {group.State} {group.IsSimpleConsumerGroup}");

        }


        return response;

    }
}

