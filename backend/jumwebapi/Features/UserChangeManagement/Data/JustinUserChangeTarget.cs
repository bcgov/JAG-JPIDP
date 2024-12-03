namespace jumwebapi.Features.UserChangeManagement.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using jumwebapi.Models;
using NodaTime;

[Table(nameof(JustinUserChangeTarget))]
public class JustinUserChangeTarget : BaseAuditable
{
    [Key]
    public int ChangeTargetId { get; set; }
    public string ServiceName { get; set; }
    public string ChangeStatus { get; set; }
    public string ErrorDetails { get; set; }
    public Instant CompletedTime { get; set; }
    public int JustinUserChangeId { get; set; }  // Navigation property
    public JustinUserChange JustinUserChange { get; set; }  // Navigation property
}
