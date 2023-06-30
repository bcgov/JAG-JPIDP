namespace Pidp.Models;

public class EdtPersonProvisioningModel
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EdtPersonAddress? Address { get; set; } = new EdtPersonAddress();
    public string? Role { get; set; } = "Participant";
    public bool? IsActive => true;
    public string? SystemName { get; set; } = "DigitalEvidenceDefence";
    public int AccessRequestId { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationName { get; set; }
    public List<EdtField> Fields { get; set; } = new List<EdtField>();
}

public class EdtPersonAddress
{
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
}

public class EdtField
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}
