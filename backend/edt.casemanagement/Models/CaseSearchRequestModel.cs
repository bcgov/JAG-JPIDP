namespace edt.casemanagement.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;
using NodaTime;

[Table(nameof(CaseSearchRequest))]
public class CaseSearchRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public Instant? Requested { get; set; }

    public string PartyId { get; set; }

    public string AgencyFileNumber { get; set; } = string.Empty;
    public string SearchString { get; set; } = string.Empty;


    public long ResponseTime { get; set; }

    public string ResponseStatus { get; set; }
    public string ResponseError { get; set; } = string.Empty;
}
