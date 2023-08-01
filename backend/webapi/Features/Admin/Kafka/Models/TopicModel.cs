namespace Pidp.Features.Admin.Kafka.Models;

public class TopicModel
{
    public string Name { get; set; } = string.Empty;
    public int Partitions { get; set; }
    public int Replicas { get; set; }
    public int InSyncReplicas { get; set; }
    public int Entries { get; set; }
    public List<string> Consumers { get; set; } = new List<string>();
}
