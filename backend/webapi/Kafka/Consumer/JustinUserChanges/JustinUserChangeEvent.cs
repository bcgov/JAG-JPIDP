namespace Pidp.Kafka.Consumer.JustinUserChanges;

using NodaTime;
using System.Text.Json.Serialization;

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
    public string Source { get; set; } = string.Empty;
}

