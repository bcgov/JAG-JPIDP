using jumwebapi.Models;

namespace jumwebapi.Data.ef;

[Table(nameof(ParticipantMerge))]
public class ParticipantMerge : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public DateTime MergeEventTime { get; set; }
    public string SourceParticipantId { get; set; } = string.Empty;
    public string TargetParticipantId { get; set; } = string.Empty;

}
