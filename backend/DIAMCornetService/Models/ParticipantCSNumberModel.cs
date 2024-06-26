namespace DIAMCornetService.Models;

public class ParticipantCSNumberModel
{
    public string? CSNumber { get; set; }
    public string? ParticipantId { get; set; }
    public string? ErrorMessage { get; set; }
    public CornetCSNumberErrorType? ErrorType { get; set; }
}

public enum CornetCSNumberErrorType
{
    missingCSNumber,
    noActiveBioMetrics,
    eDisclosureNotProvisioned,
    unknownResponseError,
    otherError
}
