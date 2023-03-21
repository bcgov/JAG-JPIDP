namespace NotificationService.Models;

using NotificationService.HttpClients.Mail;

public class DomainEventEmail
{
    public Email? EMail { get; set; }
    public string DomainEvent { get; set; } = string.Empty;
    public Dictionary<string, string> EventData { get; set; } = new Dictionary<string, string>();

}
