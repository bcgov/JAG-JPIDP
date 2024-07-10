namespace DIAMCornetService.Services;

using DIAMCornetService.Models;

public interface ICornetORDSClient
{
    // get a cs number for a participant
    Task<ParticipantResponseModel> GetCSNumberForParticipantAsync(string participantId);

    // send a notification to a participant
    Task<string> SubmitParticipantNotificationAsync(ParticipantResponseModel model, string messageText);
}
