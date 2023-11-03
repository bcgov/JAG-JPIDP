namespace NotificationService.HttpClients.Mail;

public interface ISmtpEmailClient
{
    Task SendAsync(Email email);
}
