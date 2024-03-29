namespace Pidp.Models;

public class UserTypeModel
{
    public string OrganizationType { get; set; } = string.Empty;
    public string OrganizationName { get;set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;

    public bool IsSubmittingAgency { get; set; }

    public string SubmittingAgencyCode { get; set; } = string.Empty;
}
