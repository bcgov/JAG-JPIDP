using NodaTime;
using NotificationService.HttpClients.Mail;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationService.NotificationEvents.UserProvisioning.Models;
[Table(nameof(EmailLog))]
public class EmailLog : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    public string SendType { get; set; } = string.Empty;

    public Guid? MsgId { get; set; }
    public string Tag { get; set; } = String.Empty;

    public string SentTo { get; set; } = string.Empty;

    public string Cc { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public Instant? DateSent { get; set; }

    public string? LatestStatus { get; set; }

    public string? StatusMessage { get; set; }

    public int UpdateCount { get; set; }

    public EmailLog() { }

    public EmailLog(Email email, string sendType, Guid? msgId, string? tag, Instant dateSent)
    {
        this.Body = email.Body;
        this.Cc = string.Join(",", email.Cc);
        this.DateSent = dateSent;
        this.MsgId = msgId;
        this.SendType = sendType;
        this.SentTo = string.Join(",", email.To);
        this.Subject = email.Subject;
        this.Tag = tag;
    }
}