namespace MessagingAdapter.Configuration;

public class KafkaTargetOptions
{
    public const string KafkaTargets = "KafkaTargets";
    public Target[] Targets { get; set; }
}

public class Target
{
    public string MessageType { get; set; } = string.Empty;
    public string TargetTopic { get; set; } = string.Empty;

}
