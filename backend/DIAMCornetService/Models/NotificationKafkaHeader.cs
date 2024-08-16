namespace DIAMCornetService.Models;

public class NotificationKafkaHeader
{
    public required string MessageKey { get; set; }
    public required string CorrelationId { get; set; }
}
