namespace edt.service.Kafka.Model;

using Newtonsoft.Json;



/// <summary>
/// Represents a change to a user in relation to EDT
/// Avro schema will automatically register with pascal names which causes issues on the
/// consuming side. For now we'll use camelCase naming as the AVRO register will automatically take the names of the fields
/// regardless of any json conversions.
/// </summary>
#pragma warning disable IDE1006 // Naming Styles

public class UserModificationEvent : AuditEvent
{

    public enum UserEvent
    {
        Create,
        Modify,
        Delete,
        Disable,
        Enable,
        EnableTombstone
    }

    public string partId { get; set; } = string.Empty;

    public UserEvent eventType { get; set; } = UserEvent.Create;

    public int accessRequestId { get; set; }

    public bool successful { get; set; }
    public bool submittingAgencyUser { get; set; }


    public override string ToString() => JsonConvert.SerializeObject(this);



}
