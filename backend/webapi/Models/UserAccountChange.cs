namespace Pidp.Models;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table(nameof(UserAccountChange))]
public class UserAccountChange : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public int PartyId { get; set; }

    public Party? Party { get; set; }

    public bool Deactivated { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string ChangeData { get; set; } = string.Empty;

}
