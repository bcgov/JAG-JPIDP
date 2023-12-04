namespace edt.service.HttpClients.Services.EdtCore;

using Common.Models.EDT;

public class EdtPersonProvisioningModel
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EdtPersonAddress? Address { get; set; }
    public string? Role { get; set; } = "Participant";
    public bool? IsActive => true;
    public string? SystemName { get; set; } = "DigitalEvidenceDefence";
    public int ManuallyAddedParticipantId { get; set; } = -1;
    public string EdtExternalIdentifier { get; set; } = string.Empty;
    public int AccessRequestId { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationName { get; set; }
    public List<EdtField> Fields { get; set; } = new List<EdtField>();
}


