namespace DIAMCornetService.Services;

public interface ICornetORDSClient
{
    // get a cs number for a participant
    Task<string> GetCSNumberForParticipant(string participantId);
}
