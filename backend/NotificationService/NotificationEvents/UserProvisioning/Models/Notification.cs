namespace NotificationService.NotificationEvents.UserProvisioning.Models;
public class Notification
{
    public string? To { get; set; }
    public string? From { get; set; }
    public string? FirstName { get; set; }
    public string? Subject { get; set; }
    public string? MsgBody { get; set; }
    public string ParyId { get; set; } = string.Empty;
    public string? Tag { get; set; }
}

