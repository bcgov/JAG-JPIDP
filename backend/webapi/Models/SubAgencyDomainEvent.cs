namespace Pidp.Models;

using NodaTime;

public class SubAgencyDomainEvent
{
    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public string? Username { get; set; }
    public string AgencyFileNumber { get; set; } = string.Empty;
    public int CaseId { get; set; }
    public Instant RequestedOn { get; set; }
}
