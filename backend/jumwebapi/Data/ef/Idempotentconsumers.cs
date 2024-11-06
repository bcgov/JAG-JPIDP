namespace jumwebapi.Data.ef;

using System.ComponentModel.DataAnnotations;
using NodaTime;


[Table(nameof(IdempotentConsumers))]
public class IdempotentConsumers
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public Instant ConsumeDate { get; set; }

}
