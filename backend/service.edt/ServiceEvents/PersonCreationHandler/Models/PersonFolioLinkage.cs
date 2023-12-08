namespace edt.service.ServiceEvents.PersonCreationHandler.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


[Table(nameof(PersonFolioLinkage))]
public class PersonFolioLinkage : UserAccountCreation.Models.BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string PersonKey { get; set; } = string.Empty;
    [Required]
    public string DisclosureCaseIdentifier { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string PersonType { get; set; } = string.Empty;
}
