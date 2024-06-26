namespace DIAMCornetService.Services;

using DIAMCornetService.Models;

public interface ICornetService
{
    public Task<ParticipantCSNumberModel> GetParticipantCSNumberAsync(string participantId);
    public Task<int> SubmitNotificationToEServices(string csNumber, string message);
    public Task<Dictionary<string, string>> PublishCSNumberResponseAsync(string participantId);
}
