namespace Pidp.Models.OutBoxEvent;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NodaTime;

[Table(nameof(ExportedEvent))]
public class ExportedEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventId { get; set; }
    [Required]
    public string AggregateType { get; set; } = string.Empty;
    [Required]
    public string? AggregateId { get; set; }
    [Required]
    public string EventType { get; set; } = string.Empty;
    [Required]
    ///
    /// replace with jsonb dataType see https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
    ///
    [Column(TypeName = "jsonb")]
    public string? EventPayload { get; set; }
    public Instant DateOccurred { get; set; }

}
