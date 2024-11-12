namespace Pidp.Models;
public class EdtDisclosureUserProvisioning
{
    public string? Key { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool? IsActive => true;
    public string? AccountType { get; set; }
    public string? OrganizationType { get; set; }
    public List<string> DisclosurePortalCaseIds { get; set; } = [];
    public string? OrganizationName { get; set; }
    public string? SystemName { get; set; } = "DISCLOSURE";
    public int AccessRequestId { get; set; }
    public string PersonKey { get; set; } // ties to participant unique id in core for disclosure
}
