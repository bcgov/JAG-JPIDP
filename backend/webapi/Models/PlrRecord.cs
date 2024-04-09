namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(PlrRecord))]
public class PlrRecord : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    // dummy table to resolve original reliance on PLR code
}
