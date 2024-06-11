namespace ISLInterfaces.Features.Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Party")]
public class PartyModel
{
    [Key]
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? Jpdid { get; set; }
}
