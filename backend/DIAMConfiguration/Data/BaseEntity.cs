namespace DIAMConfiguration.Data;

using System.ComponentModel.DataAnnotations;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset? Deleted { get; set; }
    [Required]
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Modified { get; set; }

}
