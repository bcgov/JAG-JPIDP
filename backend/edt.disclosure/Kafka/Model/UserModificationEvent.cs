namespace edt.disclosure.Kafka.Model;

using Newtonsoft.Json;

public class UserModificationEvent : AuditEvent
{

    public enum UserEvent
    {
        Create,
        Modify,
        Delete,
        Disable,
        Enable
    }

    public string partId { get; set; } = string.Empty;

    public UserEvent eventType { get; set; } = UserEvent.Create;

    public int accessRequestId { get; set; }

    public bool successful { get; set; }
    public List<string> Errors { get; set; } = new List<string>();


    public override string ToString() => JsonConvert.SerializeObject(this);
}
