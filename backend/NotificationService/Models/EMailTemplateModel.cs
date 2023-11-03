namespace NotificationService.Models;

public class EMailTemplateModel
{
    public bool IsUrgent { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
}
