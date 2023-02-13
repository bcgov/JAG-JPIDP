using NotificationService.HttpClients.Mail;

namespace NotificationService.Services;
    public interface IEmailService
    {
        Task<Guid?> SendAsync(Email email, string tag);
        Task<int> UpdateEmailLogStatuses(int limit);
    }


