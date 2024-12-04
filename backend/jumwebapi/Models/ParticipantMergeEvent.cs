namespace jumwebapi.Models;

public class ParticipantMergeEvent
{
    public DateTime MergeEventTime { get; set; }
    public string SourceParticipantId { get; set; } = string.Empty;
    public string TargetParticipantId { get; set; } = string.Empty;
}
