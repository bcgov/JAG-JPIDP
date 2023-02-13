using System.ComponentModel.DataAnnotations;

namespace NotificationService.NotificationEvents.UserProvisioning.Models;
public class NotificationAckModel
{
    public Guid NotificationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string EmailAddress { get; set; } =string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public int AccessRequestId { get; set; }
}

