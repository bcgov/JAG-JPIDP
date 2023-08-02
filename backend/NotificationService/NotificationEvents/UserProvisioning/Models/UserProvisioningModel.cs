namespace NotificationService.NotificationEvents.UserProvisioning.Models;
public class UserProvisioningModel
{
    public string UserName { get; set; } = string.Empty;
    public bool IsDisable { get; set; }
    public long ParticipantId { get; set; }
    //public Guid DigitalIdentifier { get; set; } = default;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string PreferredName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public long AgencyId { get; set; }
    public PartyTypeCode PartyTypeCode { get; set; }
    public IEnumerable<RoleModel> Roles { get; set; } = new List<RoleModel>();
}

public class RoleModel
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public bool IsDisable { get; set; }
}

public enum PartyTypeCode
{
    Organization = 1,
    Individual = 2,
    Staff = 3,
}