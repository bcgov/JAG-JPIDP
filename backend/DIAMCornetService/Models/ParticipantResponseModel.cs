namespace DIAMCornetService.Models;

public class ParticipantResponseModel
{
    public string? CSNumber { get; set; }
    public string? ParticipantId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DIAMPublishId { get; set; }
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
