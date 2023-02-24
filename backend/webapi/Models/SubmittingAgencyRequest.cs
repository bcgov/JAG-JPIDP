namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

[Table(nameof(SubmittingAgencyRequest))]
public class SubmittingAgencyRequest : BaseAuditable
{
    [Key]
    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public Party? Party { get; set; }
    [Required]
    public string CaseNumber { get; set; } = string.Empty;
    [Required]
    public string AgencyCode { get; set; } = string.Empty;
    [Required]
    public string CaseGroup { get; set; } = string.Empty;
    [Required]
    public string CaseName { get; set; } = string.Empty;
    public Instant RequestedOn { get; set; }
    [Required]
    public string RequestStatus { get; set; } = AgencyRequestStatus.Submitted;
    public ICollection<AgencyRequestAttachment> AgencyRequestAttachments { get; set; } = new List<AgencyRequestAttachment>();
}
public static class AgencyRequestStatus
{
    public const string Accepted = "Accepted";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Pending = "Pending";
    public const string RequestQueued = "Request Queued";
    public const string DeleteQueued = "Delete Queued";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Dismissed = "Dismissed";
    public const string Expired = "Expired";
}
