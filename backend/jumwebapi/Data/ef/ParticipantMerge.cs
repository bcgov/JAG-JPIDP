using jumwebapi.Models;

namespace jumwebapi.Data.ef;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(ParticipantMerge))]
public class ParticipantMerge : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public DateTime MergeEventTime { get; set; }
    public string SourceParticipantId { get; set; } = string.Empty;
    public string TargetParticipantId { get; set; } = string.Empty;
    public string SourceParticipantFirstName { get; set; } = string.Empty;
    public string SourceParticipantLastName { get; set; } = string.Empty;
    public string TargetParticipantFirstName { get; set; } = string.Empty;
    public string TargetParticipantLastName { get; set; } = string.Empty;
    public DateOnly SourceParticipantDOB { get; set; }
    public DateOnly TargetParticipantDOB { get; set; }
    public string PublishedMessageId { get; set; } = string.Empty;
    public string Errors { get; set; } = string.Empty;
}
