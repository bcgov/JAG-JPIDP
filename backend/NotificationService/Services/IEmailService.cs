namespace NotificationService.Services;

using NotificationService.Models;
using NotificationService.NotificationEvents.UserProvisioning.Models;

public interface IEmailService
{
    Task<Guid?> SendAsync(Notification email);
    Task<int> UpdateEmailLogStatuses(int limit);


}


