namespace DIAMCornetService.Models;

using System.ComponentModel.DataAnnotations;

public class IncomingMessage
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public DateTime MessageTimestamp { get; set; }
    public string CSNumber { get; set; } = string.Empty;
    public DateTime CompletedTimestamp { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProcessResponseId { get; set; } = string.Empty;
}
