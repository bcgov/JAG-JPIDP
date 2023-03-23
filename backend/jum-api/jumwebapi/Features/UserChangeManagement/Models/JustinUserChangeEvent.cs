namespace jumwebapi.Models;

using NodaTime;

/// <summary>
/// Represents a user change event captured from JUSTIN
/// Current we dont know the changes only that something has changed
/// </summary>
public class JustinUserChangeEvent
{
    public string PartId { get; set; } = string.Empty;
    public Instant EventTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int EventMessageId { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new Dictionary<string, object>();
}
