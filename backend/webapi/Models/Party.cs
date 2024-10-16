namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pidp.Models.UserInfo;

[Table(nameof(Party))]
public class Party : BaseAuditable, IOwnedResource
{
    [Key]
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string? Jpdid { get; set; }

    public DateOnly? Birthdate { get; set; }

    public string? Gender { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PreferredFirstName { get; set; }

    public string? PreferredMiddleName { get; set; }

    public string? PreferredLastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public PartyAccessAdministrator? AccessAdministrator { get; set; }

    public string? Cpn { get; set; }

    public PartyLicenceDeclaration? LicenceDeclaration { get; set; }

    public string? JobTitle { get; set; }

    public Facility? Facility { get; set; }

    public PartyOrgainizationDetail? OrgainizationDetail { get; set; }

    //public CorrectionServiceDetail? CorrectionServiceDetail { get; set; }

    public ICollection<AccessRequest> AccessRequests { get; set; } = new List<AccessRequest>();

    public ICollection<UserAccountChange> AccountChanges { get; set; } = new List<UserAccountChange>();

    public ICollection<PartyAlternateId> AlternateIds { get; set; } = new List<PartyAlternateId>();

    public ICollection<PartyUserType> PartyUserTypes { get; set; } = new List<PartyUserType>();


    public ICollection<PublicUserValidation> ValidationAttempts { get; set; } = new List<PublicUserValidation>();


}
