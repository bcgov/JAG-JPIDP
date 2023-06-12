namespace edt.disclosure.ServiceEvents.UserAccountCreation.Models;

using System.ComponentModel.DataAnnotations;
using NodaTime;

public class IdempotentConsumer
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public Instant ConsumeDate { get; set; }
}
