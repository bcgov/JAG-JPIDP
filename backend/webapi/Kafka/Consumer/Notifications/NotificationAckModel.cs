namespace Pidp.Kafka.Consumer.Notifications;
public class NotificationAckModel
{
    public string NotificationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public int AccessRequestId { get; set; }
    public string Details { get; set; } = string.Empty;
    public NotificationSubject Subject { get; set; } = NotificationSubject.None;
    public string EventType { get; set; } = string.Empty;
}

public enum NotificationSubject
{
    AccessRequest,
    CaseAccessRequest,
    None
}
