namespace ApprovalFlow.Data.Approval;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;

/// <summary>
/// An ApprovalRequest can contain one or more requests, this allows us to group requests for a user
/// together. E.g. a lawyer might be requesting to be a participant in core and user in disclosure.
/// Or in future a user may request to have access to something and also change their email address
/// </summary>
[Table(nameof(Request))]
public class Request : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; } // navigation property
    public int RequestId { get; set; }

    public ApprovalType ApprovalType { get; set; } = ApprovalType.AccessRequest;
    public string RequestType { get; set; } = string.Empty;
    public ICollection<ApprovalHistory> History { get; set; }

}

public enum ApprovalType
{
    AccessRequest,
    AccountChange
}
