namespace NotificationService.Services;

using NotificationService.Models;

public interface IEmailTemplateCache
{
    /// <summary>
    /// For a given domain event object get the applicable template
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    Task<EMailTemplateModel> GetEmailTemplate(string identifier);
}
