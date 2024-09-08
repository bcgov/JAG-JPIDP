namespace DIAMCornetService.Services;

public interface INotificationService
{
    public Task<string> PublishTestNotificationAsync(string participantId, string messageText);
}
