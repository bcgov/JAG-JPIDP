namespace Common.Models.EDT;

using System.Text.Json;

public abstract class EdtDisclosureUserProvisioningModel
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; } = true;
    public string? AccountType { get; set; }
    public string? SystemName { get; set; } = "DigitalEvidenceDisclosure";
    public int AccessRequestId { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationName { get; set; }
    public int ManuallyAddedParticipantId { get; set; } = -1;
    public string EdtExternalIdentifier { get; set; } = string.Empty;
    public override string ToString() => JsonSerializer.Serialize(this);

}

public class EdtDisclosureDefenceUserProvisioningModel : EdtDisclosureUserProvisioningModel
{

    public EdtDisclosureDefenceUserProvisioningModel()
    {
        this.OrganizationName = "Defence";
        this.OrganizationName = "BC Law";
    }
}

public class EdtDisclosurePublicUserProvisioningModel : EdtDisclosureUserProvisioningModel
{
    public string PersonKey { get; set; } = string.Empty; // ties to participant unique id in core for disclosure for public users

    public EdtDisclosurePublicUserProvisioningModel()
    {
        this.OrganizationName = "Public";
        this.OrganizationName = "Out-of-custody";
    }

}

public class CaseId
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;

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

