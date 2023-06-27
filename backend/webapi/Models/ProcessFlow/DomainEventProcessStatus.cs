namespace Pidp.Models.ProcessFlow;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(DomainEventProcessStatus))]
public class DomainEventProcessStatus : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public int RequestId { get; set; }

    public ProcessFlowEvent? ProcessFlowEvent { get; set; }

    public ProcessFlowStatus? Status { get; set; }

    public string Errors { get; set; } = string.Empty;


}

public enum ProcessFlowStatus
{
    Complete,
    Error,
    Pending,
    Incomplete

}
