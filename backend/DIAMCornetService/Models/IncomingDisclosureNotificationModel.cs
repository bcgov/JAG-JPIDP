namespace DIAMCornetService.Models;

public class IncomingDisclosureNotificationModel
{
    public string NotificationId { get; set; } = Guid.NewGuid().ToString();
    public required DateTime NotificationDateUTC { get; set; }
    public string MessageText { get; set; } = "No message provided";
    public required string ParticipantId { get; set; }
    public DisclosureEventTypes EventType { get; set; }

}


public enum DisclosureEventTypes
{
    DISCLOSURE_CREATION,
    DISCLOSURE_DELETION
}
