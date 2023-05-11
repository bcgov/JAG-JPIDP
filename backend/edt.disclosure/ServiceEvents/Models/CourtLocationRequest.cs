namespace edt.disclosure.ServiceEvents.Models;

using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

[Table(nameof(CourtLocationRequest))]
public class CourtLocationRequest : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    public Instant? Requested { get; set; }
    public bool RemoveRequested { get; set; }
    public int CaseId { get; set; }
    public Instant? DeletedOn { get; set; }
    public int PartyId { get; set; }
    public string Location { get; set; }
    public string? SubLocation { get; set; }

    public string status { get; set; }
}
