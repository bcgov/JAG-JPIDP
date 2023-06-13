namespace edt.disclosure.HttpClients.Services.EdtDisclosure;


using System.Text.Json;

public class EdtDisclosureUserProvisioningModel
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool? IsActive => true;
    public string? AccountType { get; set; }
    public string? SystemName { get; set; } = "DigitalEvidenceDisclosure";
    public int AccessRequestId { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationName { get; set; }
    public string FolioId { get; set; }
    public int FolioCaseId { get; set; }    
    public override string ToString() => JsonSerializer.Serialize(this);

}



/// <summary>
/// Represents a response from EDT API for group assignment for a user
/// </summary>
public class EdtUserGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString() => JsonSerializer.Serialize(this);

}



