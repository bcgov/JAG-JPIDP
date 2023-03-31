namespace NotificationService.HttpClients.Mail;

public interface IChesClient
{
    Task<EmailSuccessResponse?> SendAsync(Email email);
    Task<string?> GetStatusAsync(Guid msgId);
    Task<bool> HealthCheckAsync();
}
