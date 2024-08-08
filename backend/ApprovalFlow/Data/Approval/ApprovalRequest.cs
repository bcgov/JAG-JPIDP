namespace ApprovalFlow.Data.Approval;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;
using NodaTime;

[Table(nameof(ApprovalRequest))]
public class ApprovalRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string MessageKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
    public int NoOfApprovalsRequired { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string EMailAddress { get; set; } = string.Empty;
    public string RequiredAccess { get; set; } = string.Empty;
    public Instant? Approved { get; set; }
    public Instant? Completed { get; set; }
    public ICollection<PersonalIdentity> PersonalIdentities { get; set; } = [];
    public ICollection<ApprovalRequestReasons> Reasons { get; set; } = [];

    public ICollection<Request> Requests { get; set; } = [];

}

[Table(nameof(ApprovalRequestReasons))]
public class ApprovalRequestReasons : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
    public ApprovalRequest ApprovalRequest { get; set; } = default!;
    public int ApprovalRequestId { get; set; }


}



