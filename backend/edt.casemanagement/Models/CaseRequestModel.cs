namespace edt.casemanagement.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using edt.casemanagement.Data;
using NodaTime;

[Table(nameof(CaseRequest))]
public class CaseRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public Instant? Requested { get; set; }
    public bool RemoveRequested { get; set; }

    public int CaseId { get; set; }
    public string Details { get; set; } = string.Empty;
    public int PartyId { get; set; }
    public string Party { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AgencFileNumber { get; set; } = string.Empty;
}
