namespace Pidp.Models.ProcessFlow;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pidp.Models.Lookups;

[Table(nameof(ProcessFlow))]
public class ProcessFlow : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Column(TypeName = "numeric(3, 2)")]
    public double Sequence { get; set; }
    public ProcessSection? ProcessSection { get; set; }
    public bool IsLocked { get; set; }
    public string IdentityProvider { get; set; } = string.Empty;
    public AccessTypeCode AccessTypeCode { get; set; }
}

