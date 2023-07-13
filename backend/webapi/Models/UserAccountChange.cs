namespace Pidp.Models;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using NodaTime;

[Table(nameof(UserAccountChange))]
public class UserAccountChange : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public int PartyId { get; set; }

    public int EventMessageId { get; set; }

    public Party? Party { get; set; }

    public bool Deactivated { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string ChangeData { get; set; } = string.Empty;

    public Instant Completed { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

}

