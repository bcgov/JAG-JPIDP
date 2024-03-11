namespace Pidp.Models.UserInfo;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table(nameof(PartyUserType))]
public class PartyUserType : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public int PartyId { get; set; }
    public Party Party { get; set; }
    public int UserTypeLookupId { get; set; }
    public UserTypeLookup UserTypeLookup { get; set; }

}

[Table(nameof(UserTypeLookup))]
public class UserTypeLookup : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

}

public enum PublicUserType
{
    OutOfCustodyAccused,
    InCustodyAccused
}
