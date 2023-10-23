namespace Pidp.Models.UserInfo;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;

[Table(nameof(PublicUserValidation))]
public class PublicUserValidation : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public int PartyId { get; set; }
    public Party? Party { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsValid { get; set; }
}
