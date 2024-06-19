namespace DIAMCornetService.Services;

using DIAMCornetService.Models;

public interface ICornetService
{
    public Task<ParticipantCSNumberModel> GetParticipantCSNumberAsync(string participantId);
    public Task<int> PublishNotificationAsync(string csNumber, string message);
    public Task<Dictionary<string, string>> PublishCSNumberResponseAsync(string participantId);
}
