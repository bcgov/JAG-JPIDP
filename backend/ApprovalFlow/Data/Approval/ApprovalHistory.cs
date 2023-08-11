namespace ApprovalFlow.Data.Approval;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Models.Approval;
using DIAM.Common.Models;
using NodaTime;

[Table(nameof(ApprovalHistory))]
public class ApprovalHistory : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public string DecisionNote { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public Request AccessRequest { get; set; }
    public Instant? Deleted { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.PENDING;

}
