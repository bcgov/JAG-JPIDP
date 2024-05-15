namespace edt.notifications.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class DisclosureEventModel : EventModel
{
    public DisclosureEventType DisclosureEventType { get; set; } = DisclosureEventType.ExportToParticipant;
    public int PersonId { get; set; }
    public int FromCaseId { get; set; }
    public int ToCaseId { get; set; }
    public int ToInstanceId { get; set; }
    public string PersonKey { get; set; } = string.Empty;
    public DisclosureParticipantType DisclosureParticipantType { get; set; } = DisclosureParticipantType.Accused;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DisclosureEventType
{
    ExportToParticipant,
    ExportToParticipantDeleted
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DisclosureParticipantType
{
    Accused,
    DefenceCounsel
}
