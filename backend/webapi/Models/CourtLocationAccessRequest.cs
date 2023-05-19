namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using Pidp.Models.Lookups;

[Table(nameof(CourtLocationAccessRequest))]
public class CourtLocationAccessRequest : BaseAuditable
{
    [Key]
    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? MessageId { get; set; }
    public CourtLocation? CourtLocation { get; set; }
    public CourtSubLocation? CourtSubLocation { get; set; }
    [Required]
    public Instant RequestedOn { get; set; }
    [Required]
    public string RequestStatus { get; set; } = CourtLocationAccessStatus.Submitted;
    public Instant? DeletedOn { get; set; }
    [Required]
    public DateTime ValidFrom { get; set; }
    [Required]
    public DateTime ValidUntil { get; set; }
}
public static class CourtLocationAccessStatus
{
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Pending = "Pending";
    public const string Queued = "In Progress";
    public const string Submitted = "Submitted";
    public const string SubmittedFuture = "Future";
    public const string Approved = "Approved";
    public const string Deleted = "Deleted";
    public const string RemovalPending = "Removal Pending";
}
