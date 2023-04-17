namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

[Table(nameof(FutureUserChangeEvent))]
public class FutureUserChangeEvent : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public int PartyId { get; set; }
    public Party? Party { get; set; }

    public Instant EventDate { get; set; }

    public Instant Completed { get; set; }

}
