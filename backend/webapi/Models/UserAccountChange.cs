namespace Pidp.Models;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table(nameof(UserAccountChange))]
public class UserAccountChange : BaseAuditable
{
    [Key]
    public int Id { get; set; }



}
