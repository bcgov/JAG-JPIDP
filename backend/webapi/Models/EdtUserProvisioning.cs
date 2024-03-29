namespace Pidp.Models;

public class EdtUserProvisioning
{
    public string? Key { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Role { get; set; }
    public List<AssignedRegion> AssignedRegions { get; set; } = new List<AssignedRegion>();
    public bool? IsActive => true;
    public string? AccountType { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationName { get; set; }
    public string? SystemName { get; set; } = "DEMS";

    public int AccessRequestId { get; set; }
}
