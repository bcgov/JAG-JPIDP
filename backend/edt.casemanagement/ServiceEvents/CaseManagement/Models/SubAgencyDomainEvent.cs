namespace edt.casemanagement.ServiceEvents.CaseManagement.Models;

using NodaTime;

public class SubAgencyDomainEvent
{
    public int RequestId { get; set; }
    public int PartyId { get; set; }
    public string? Username { get; set; }

    public Guid UserId { get; set; }

    public string AgencyFileNumber { get; set; } = string.Empty;
    public int CaseId { get; set; }
    public string EventType { get; set; } = CaseEventType.None;

    public DateTimeOffset RequestedOn { get; set; }
}

public static class CaseEventType
{
    public const string Provisioning = "Provisioning";
    public const string Decommission = "Decommission";
    public const string None = "None";
}
