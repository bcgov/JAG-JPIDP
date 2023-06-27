namespace Pidp.Models.ProcessFlow;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(ProcessFlowEvent))]
public class ProcessFlowEvent : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public ProcessFlow? ProcessFlow { get; set; }

    public string FromDomainEvent { get; set; } = string.Empty;
    public string ToDomainEvent { get; set;} = string.Empty;

}
