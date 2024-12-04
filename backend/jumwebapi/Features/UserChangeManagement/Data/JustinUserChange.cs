namespace jumwebapi.Features.UserChangeManagement.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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


    public Instant Completed { get; set; }

    [InverseProperty("JustinUserChange")]
    public ICollection<JustinUserChangeTarget> TargetChanges { get; set; }
}
