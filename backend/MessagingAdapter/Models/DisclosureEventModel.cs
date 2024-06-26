namespace MessagingAdapter.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Represents a disclosure event
/// e.g.
/**
 * 
 * {
  "Id": 222,  
  "CreatedUtc": "2024-03-16T12:34:56.888Z", 
  "CreatedByUsername": "jblogs",      
  "EventType": "ExportToParticipant", 
  "OrgUnitID": 1   ,
  "CaseID": 1234 ,
  "Fields" : [
     { "name": "ExportToParticipantId", "value": "1" },
     { "name": "ExportToParticipantCreatedUtc", "value": "2024-03-16T12:33:44.555Z" },
     { "name": "ToEdtInstanceId", "value": "2" },
     { "name": "ToCaseId", "value": "1234" },
     { "name": "PersonId", "value": "9999" },
     { "name": "PersonKey", "value": "XYZ-123" },  
     { "name": "ParticipantId", "value": "888" },
     { "name": "ParticipantType", "value": "Accused" }
  ]

} */
/// </summary>
public class DisclosureEventModel : EventModel
{
    public DisclosureEventType EventType { get; set; } = DisclosureEventType.ExportToParticipant;
    public int OrgUnitId { get; set; }
    public int FromCaseId { get; set; }
    public int CaseID { get; set; }
}



[JsonConverter(typeof(StringEnumConverter))]
public enum DisclosureEventType
{
    ExportToParticipant,
    ExportToParticipantDeleted
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DisclosureKeyFields
{
    ExportToParticipantId,
    ExportToParticipantCreatedUtc,
    ExportToParticipantDeletedUtc,
    ToEdtInstanceId,
    PersonKey, // participant ID from JUSTIN
    ToCaseId,
    PersonId,
    ParticipantId,
    ParticipantType
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DisclosureParticipantType
{
    Accused,
    DefenceCounsel
}
