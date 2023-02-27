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
    public int CaseId { get; set; }
    [Required]
    public string AgencyFileNumber { get; set; } = string.Empty;
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
    public const string Queued = "Queued";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Dismissed = "Dismissed";
    public const string Expired = "Expired";
}
