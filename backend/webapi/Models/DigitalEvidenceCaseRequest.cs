namespace Pidp.Models;

using NodaTime;

public class DigitalEvidenceCaseRequest
{
    public int Id { get; set; }

    public int CaseId { get; set; }
    public int RequestingPartyId { get; set; }

    public Instant RequestedDate { get; set; }
    public Instant GrantedDate { get; set; }


}
