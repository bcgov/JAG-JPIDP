using System.ComponentModel.DataAnnotations;

namespace NotificationService.NotificationEvents.UserProvisioning.Models;
public class NotificationAckModel
{
    public Guid NotificationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public int AccessRequestId { get; set; }
    public string DomainEvent { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;

    public NotificationSubject Subject { get; set; } = NotificationSubject.None;

}

public enum NotificationSubject
{
    AccessRequest,
    CaseAccessRequest,
    None
}
