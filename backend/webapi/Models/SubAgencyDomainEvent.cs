namespace Pidp.Models;

using NodaTime;

public class SubAgencyDomainEvent
{
    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public string? Username { get; set; }
    public string AgencyCode { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string CaseGroup { get; set; } = string.Empty;
    public Instant RequestedOn { get; set; }
}
