namespace ApprovalFlow.Data.Approval;

using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DIAM.Common.Models;

[Table(nameof(ApprovalRequest))]
public class ApprovalRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    [Required]
    public string MessageKey { get; set; }
    public string UserId { get; set; }
    public string IdentityProvider { get; set; }
    public Instant? Approved { get; set; }
    public Instant? Completed { get; set; }
    public ICollection<Request> Requests { get; set; } = new List<Request>();

}
