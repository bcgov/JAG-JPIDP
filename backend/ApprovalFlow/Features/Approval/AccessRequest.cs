namespace ApprovalFlow.Features.Approval;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;

[Table(nameof(AccessRequest))]
public class AccessRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int ApprovalRequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
}
