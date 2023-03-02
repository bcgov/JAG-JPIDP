using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace edt.casemanagement.ServiceEvents.UserAccountCreation.Models;
public class NotificationAckModel
{

    public Guid NotificationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string EmailAddress { get; set; } =string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public int AccessRequestId { get; set; }

    public NotificationSubject Subject { get; set; } = NotificationSubject.None;
    public string EventType { get; set; } = string.Empty;

}

public enum NotificationSubject
{
    AccessRequest,
    CaseAccessRequest,
    None
}
