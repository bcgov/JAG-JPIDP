namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;

[Table(nameof(SubmittingAgencyRequest))]
[Index(nameof(RCCNumber), IsUnique = false, Name = "IX_RCCNumber")]

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

    public string? RCCNumber { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public Instant RequestedOn { get; set; }
    [Required]
    public string RequestStatus { get; set; } = AgencyRequestStatus.Submitted;
    public ICollection<AgencyRequestAttachment> AgencyRequestAttachments { get; set; } = new List<AgencyRequestAttachment>();
}
public static class AgencyRequestStatus
{
    public const string Accepted = "Accepted";
    public const string Cancelled = "Cancelled";
    public const string Complete = "Complete";
    public const string Failed = "Failed";
    public const string Pending = "Pending";
    public const string Queued = "In Progress";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Dismissed = "Dismissed";
    public const string Expired = "Expired";
    public const string RemovalPending = "Removal Pending";
}


