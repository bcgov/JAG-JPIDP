namespace DIAMCornetService.Services;

using DIAMCornetService.Models;

public interface ICornetORDSClient
{
    // get a cs number for a participant
    Task<InCustodyParticipantModel> GetCSNumberForParticipantAsync(string participantId);

    // send a notification to a participant
    Task<string> SubmitParticipantNotificationAsync(InCustodyParticipantModel model, string messageText);
}
