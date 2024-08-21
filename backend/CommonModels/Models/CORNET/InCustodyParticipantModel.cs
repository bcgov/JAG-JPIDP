namespace Common.Models.CORNET;

public class InCustodyParticipantModel
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
