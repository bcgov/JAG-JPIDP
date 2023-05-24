namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(PartyAlternateId))]
public class PartyAlternateId : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public int PartyId { get; set; }

    public Party? Party { get; set; }
}
