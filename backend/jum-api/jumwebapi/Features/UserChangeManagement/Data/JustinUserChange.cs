namespace jumwebapi.Features.UserChangeManagement.Data;

using jumwebapi.Models;
using NodaTime;

[Table(nameof(JustinUserChange))]

public class JustinUserChange : BaseAuditable
{
    [Key]
    public int EventMessageId { get; set; }
    public string PartId { get; set; }
    public Instant EventTime { get; set; }
    public string EventType { get; set; } = string.Empty;

    [InverseProperty("JustinUserChange")]
    public ICollection<JustinUserChangeTarget> TargetChanges { get; set; }
}
