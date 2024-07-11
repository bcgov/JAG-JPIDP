namespace DIAMCornetService.Models;
public class InCustodyParticipantModel
{
    public string? CSNumber { get; set; }
    public string ParticipantId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? DIAMPublishId { get; set; }
    public DateTime? CreationDateUTC { get; set; }
    public CornetCSNumberErrorType? ErrorType { get; set; }
}

public enum CornetCSNumberErrorType
{
    missingCSNumber,
    noActiveBioMetrics,
    eDisclosureNotProvisioned,
    unknownResponseError,
    participantNotFound,
    otherError
}
