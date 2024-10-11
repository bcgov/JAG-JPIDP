namespace CommonModels.Models.JUSTIN;


public class JAMProvisioningRequestModel
{
    public int AccessRequestId { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public double ParticipantId { get; set; }
    public int PartyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UPN { get; set; } = string.Empty;
    public string EventType { get; set; } = "jam-provision-request";
    public string TargetApplication { get; set; } = "JAM_POR";
}
