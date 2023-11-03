namespace Pidp.Models.OutBoxEvent;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NodaTime;

[Table(nameof(DeferredEvent))]
public class DeferredEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    public int RequestId { get; set; }
    [Required]
    public string EventType { get; set; } = string.Empty;
    [Required]
    public string Reason { get; set; } = string.Empty;
    [Required]
    ///
    /// replace with jsonb dataType see https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
    ///
    [Column(TypeName = "jsonb")]
    public string? EventPayload { get; set; }
    public Instant DateOccurred { get; set; }
    public Instant? DeferUntil { get; set; }

}
