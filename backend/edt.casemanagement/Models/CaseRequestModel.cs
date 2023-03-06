namespace edt.casemanagement.Models;

using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using edt.casemanagement.Data;

[Table(nameof(CaseRequest))]
public class CaseRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public Instant? Requested { get; set; }
    public bool RemoveRequested { get; set; }

    public int CaseId { get; set; }

    public int PartyId { get; set; }

    public string AgencFileNumber { get; set; } = string.Empty;
}
