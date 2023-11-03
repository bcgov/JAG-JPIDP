namespace Common.Models;

using System.ComponentModel.DataAnnotations;

public class IdempotentConsumer
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
}

