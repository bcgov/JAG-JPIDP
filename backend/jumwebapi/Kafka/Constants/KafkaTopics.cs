using jumwebapi.Infrastructure.Auth;

namespace jumwebapi.Kafka.Constants;
public class KafkaTopics
{
    private readonly JumWebApiConfiguration _config;
    public KafkaTopics(JumWebApiConfiguration config)
    {
        _config = config;
        UserProvisioned = config.KafkaCluster.TopicName;
    }

    private string UserProvisioned { get;set; }
}
