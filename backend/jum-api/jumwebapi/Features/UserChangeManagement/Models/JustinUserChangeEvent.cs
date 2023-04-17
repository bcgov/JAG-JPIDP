namespace jumwebapi.Models;

using System.Text.Json.Serialization;
using jumwebapi.Features.Participants.Models;
using NodaTime;

/// <summary>
/// Represents a user change event captured from JUSTIN
/// Current we dont know the changes only that something has changed
/// </summary>
public class JustinUserChangeEvent
{
    [JsonPropertyName("partId")]
    public decimal PartId { get; set; }
    [JsonPropertyName("eventTime")]
    public Instant EventTime { get; set; }
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;
    [JsonPropertyName("eventMessageId")]
    public int EventMessageId { get; set; }
    [JsonPropertyName("source")]
    public string Source { get; set; } = "JUSTIN";

}

