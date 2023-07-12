namespace Pidp.Models;

using NodaTime;
using Pidp.Models.Lookups;

public class CourtLocationAccessModel
{
    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public string Details { get; set; }
    public CourtLocation CourtLocation { get; set; }
    public CourtSubLocation CourtSubLocation { get; set; }
    public Instant RequestedOn { get; set; }
    public string RequestStatus { get; set; } = CourtLocationAccessStatus.Submitted;
    public Instant? DeletedOn { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
}
