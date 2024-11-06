using jumwebapi.Models;

namespace jumwebapi.Data.ef;

[Table(nameof(ParticipantMerges))]
public class ParticipantMerges : BaseAuditable
{
    [Key]
    public String Id { get; set; }
    public DateTime MergeEventTime { get; set; }
    public string SourceParticipantId { get; set; } = string.Empty;
    public string TargetParticipantId { get; set; } = string.Empty;
}
