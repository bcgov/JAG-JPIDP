namespace Pidp.Models;

using NodaTime;

public class CourtLocationDomainEvent
{

    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public string CourtLocationKey { get; set; } = string.Empty;
    public string CourtLocationName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public Guid UserId { get; set; }
    // todo - set court case name from EDT
    public string EventType { get; set; } = CourtLocationEventType.None;
    public Instant RequestedOn { get; set; }
    public DateTime ValidUntil { get; set; }
    public DateTime ValidFrom { get; set; }
}

public static class CourtLocationEventType
{
    public const string Provisioning = "court-location-provision";
    public const string Decommission = "court-location-decommission";
    public const string Update = "court-location-update";

    public const string None = "None";
}


