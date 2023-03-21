namespace NotificationService.NotificationEvents.UserProvisioning.Models;

using NodaTime;
using NotificationService.HttpClients.Mail;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(EmailLog))]
public class EmailLog : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public string SendType { get; set; } = string.Empty;

    // The notification ID - will be the key of the notification in the topic
    public Guid? NotificationId { get; set; }

    public Guid? SentResponseId { get; set; }


    public string SentTo { get; set; } = string.Empty;

    public string Cc { get; set; } = string.Empty;

    public Instant? DateSent { get; set; }

    public string? LatestStatus { get; set; }

    public string? StatusMessage { get; set; }

    public int UpdateCount { get; set; }

    public EmailLog() { }

    public EmailLog(Email email, string sendType, Guid? notificationId, Instant dateSent)
    {
        this.Cc = email.Cc != null ? string.Join(",", email.Cc) : "";
        this.SendType = sendType;
        this.SentTo = string.Join(",", email.To);
        this.NotificationId = notificationId;
        this.LatestStatus = ChesStatus.Pending;
    }
}
