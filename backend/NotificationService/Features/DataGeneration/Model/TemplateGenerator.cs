using NodaTime;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationService.Features.DataGeneration.Model;
[Table(nameof(TemplateGenerator))]
public class TemplateGenerator: BaseAuditable
{
    [Key]
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ServiceType { get; set; } = MailType.Mail;
    public string Extension { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateUrl { get; set; } = string.Empty;
}

public static class MailType
{
    public const string Email = "email";
    public const string Mail = "bcmail";
    public const string None = "none";
}