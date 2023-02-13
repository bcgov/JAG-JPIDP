using jumwebapi.Infrastructure.Auth;

namespace jumwebapi.Kafka.Constants;
public class KafkaTopics
{
    private readonly jumwebapiConfiguration _config;
    public KafkaTopics(jumwebapiConfiguration config)
    {
        _config = config;
        UserProvisioned = config.KafkaCluster.TopicName;
    }

    private string UserProvisioned { get;set; }
}
