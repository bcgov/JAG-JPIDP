namespace ApprovalFlow.Features.Approval;

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
    public Instant? Approved { get; set; }
    public Instant? Requested { get; set; }
    public ICollection<ApprovalHistory> History { get; }

}


