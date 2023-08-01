namespace NotificationService.NotificationEvents.UserProvisioning.Models;

public enum DeliveryMethod
{
    EMail,
    //SMS  <-- future, could be SMS, webhook, etc..
}
public class Notification
{
    public Guid NotificationId { get; set; }
    public string? To { get; set; }
    public string? From { get; set; }
    public string? Subject { get; set; }
    public string DomainEvent { get; set; } = string.Empty;
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.EMail;
    public Dictionary<string, string> EventData { get; set; } = new Dictionary<string, string>();

}

