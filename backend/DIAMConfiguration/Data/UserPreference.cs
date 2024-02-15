namespace DIAMConfiguration.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(UserPreference))]
public class UserPreference : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Column(TypeName = "jsonb")]
    public string Preference { get; set; } = string.Empty;
}
