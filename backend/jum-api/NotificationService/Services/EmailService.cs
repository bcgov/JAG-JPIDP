namespace NotificationService.Services;

using Microsoft.EntityFrameworkCore;
using NodaTime;
using NotificationService.Data;
using NotificationService.Exceptions;
using NotificationService.HttpClients.Mail;
using NotificationService.Models;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using Prometheus;
using Serilog;
using System.Linq.Expressions;

public class EmailService : IEmailService
{
    public const string NotificationServiceEmail = "justinuserprovisioning@gov.bc.ca";

    private static readonly Counter EmailSentCount = Metrics
    .CreateCounter("notification_email_sends", "Number of emails sent.");
    private static readonly Counter EmailSentFailureCount = Metrics
.CreateCounter("notification_email_send_failures", "Number of email failures.");

    private readonly IChesClient chesClient;
    private readonly IClock clock;
    private readonly Microsoft.Extensions.Logging.ILogger logger;
    private readonly ISmtpEmailClient smtpEmailClient;
    private readonly NotificationServiceConfiguration config;
    private readonly NotificationDbContext context;
    private readonly IEmailTemplateCache templateCache;


    public EmailService(
        IChesClient chesClient,
        IClock clock,
        ILogger<EmailService> logger,
        ISmtpEmailClient smtpEmailClient,
        NotificationServiceConfiguration config,
        NotificationDbContext context,
        IEmailTemplateCache templateCache)
    {
        this.chesClient = chesClient;
        this.clock = clock;
        this.logger = logger;
        this.smtpEmailClient = smtpEmailClient;
        this.config = config;
        this.context = context;
        this.templateCache = templateCache;
    }

    /// <summary>
    ///
    /// Takes a notification model and converts and sends as an email
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    public async Task<Guid?> SendAsync(Notification notification)
    {


        var domainEventEMail = await this.ConvertNotificationToDomainEventEmail(notification);

        if (!NotificationServiceConfiguration.IsProduction())
        {
            domainEventEMail.EMail.Subject = $"Testing {domainEventEMail.EMail.Subject} - please ignore this message";
        }

        if (this.config.ChesClient.Enabled && await this.chesClient.HealthCheckAsync())
        {
            //require outbox pattern here incase message got send and datastore persistent failed
            /**
             * we can use ches massage tag as random guid to string
             * store the tag in the DB before SendAsync and then
             * Query ches status using the tag whenever there's event failure to determine if a message was already sent
             * implementation coming up soon using sage or outbox pattern
             */
            var emailLogId = await this.CreateEmailLog(domainEventEMail, SendType.Ches, notification.NotificationId);
            Log.Information($"EmailLog entry [{emailLogId}] created for [{notification.NotificationId}]");


            var fakeEmailSendStr = Environment.GetEnvironmentVariable("EMAIL_NO_SEND_MODE");
            if (fakeEmailSendStr != null && fakeEmailSendStr.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                Log.Logger.Warning("*** EMAIL_NO_SEND_MODE=true - emails will not actually be sent ***");
                var fakeMsgId = Guid.NewGuid();
                await this.UpdateEmailLogSentResponseId(notification.NotificationId, fakeMsgId);

                return fakeMsgId;
            }

            try
            {
                var emailResponse = await this.chesClient.SendAsync(domainEventEMail.EMail);

                if (emailResponse != null)
                {
                    EmailSentCount.Inc();
                    var msgResponseId = emailResponse.Messages.First().MsgId;
                    Log.Information($"EmailLog successfully send for id [{emailLogId} notificationId [{notification.NotificationId}] response: [{msgResponseId}]");
                    await this.UpdateEmailLogSentResponseId(notification.NotificationId, msgResponseId);

                    return msgResponseId;
                }
                else
                {
                    EmailSentFailureCount.Inc();
                    Log.Error($"EmailLog failed to  send for [{emailLogId} [{notification.NotificationId}]");
                    await this.UpdateEmailLogSendFailures(notification.NotificationId, "No error received");

                }
            }
            catch (DeliveryException ex)
            {
                EmailSentFailureCount.Inc();
                await this.UpdateEmailLogSendFailures(notification.NotificationId, ex.Message);
            }
        }

        // Fall back to SMTP client
        //await this.CreateEmailLog(email, SendType.Smtp);
        //await this.smtpEmailClient.SendAsync(email);
        return notification.NotificationId;
    }

    private async Task<DomainEventEmail> ConvertNotificationToDomainEventEmail(Notification notification)
    {
        if (notification == null)
        {
            throw new EMailTemplateException("Null call to ConvertNotificationToDomainEventEmail");
        }

        var template = await this.templateCache.GetEmailTemplate(notification.DomainEvent);

        Log.Information("Got template {0}", template);
        return new DomainEventEmail
        {
            DomainEvent = notification.DomainEvent,

            EMail = new Email
            {
                From = string.IsNullOrEmpty(template.From) ? notification.From! : template.From,
                To = notification.To!.Split(';').AsEnumerable(),
                Subject = string.IsNullOrEmpty(template.Subject) ? notification.Subject! : template.Subject,
                Body = this.ConvertEmailBody(notification, template),
                Priority = template.IsUrgent ? "high" : "normal",
                Cc = string.IsNullOrEmpty(template.Cc) ? Enumerable.Empty<string>() : template.Cc.Split(";")
            }
        };


    }

    private string ConvertEmailBody(Notification notification, EMailTemplateModel template)
    {
        var bodyText = template.BodyText;
        foreach (var entry in notification.EventData)
        {
            bodyText = bodyText.Replace("{" + entry.Key + "}", entry.Value);
        }

        return bodyText;

    }

    private async Task<int> CreateEmailLog(DomainEventEmail email, string sendType, Guid? notificationId)
    {
        var response = this.context.EmailLogs.Add(new EmailLog(email.EMail, sendType, notificationId, this.clock.GetCurrentInstant()));

        return await this.context.SaveChangesAsync();
    }
    private async Task UpdateEmailLogSentResponseId(Guid? notificationId, Guid? responseId = null)
    {
        var emailLogs = await this.context.EmailLogs
            .Where(emailLog => emailLog.NotificationId.Equals(notificationId)).SingleOrDefaultAsync();
        if (emailLogs != null)
        {
            emailLogs.SentResponseId = responseId;
            emailLogs.LatestStatus = ChesStatus.Accepted;
            emailLogs.DateSent = this.clock.GetCurrentInstant();
        }

        await this.context.SaveChangesAsync();
    }

    private async Task UpdateEmailLogSendFailures(Guid? notificationId, string errorStr)
    {
        var emailLogs = await this.context.EmailLogs
            .Where(emailLog => emailLog.NotificationId.Equals(notificationId)).SingleOrDefaultAsync();
        if (emailLogs != null)
        {
            emailLogs.LatestStatus = ChesStatus.Failed;
            emailLogs.StatusMessage = errorStr;
        }

        await this.context.SaveChangesAsync();
    }


    public async Task<int> UpdateEmailLogStatuses(int limit)
    {
        Expression<Func<EmailLog, bool>> predicate = log =>
            log.SendType == SendType.Ches
            && log.NotificationId != null
            && log.LatestStatus != ChesStatus.Complete;

        var totalCount = await this.context.EmailLogs
            .Where(predicate)
            .CountAsync();

        var emailLogs = await this.context.EmailLogs
            .Where(predicate)
            .OrderBy(e => e.UpdateCount)
                .ThenBy(e => e.Modified)
            .Take(limit)
            .ToListAsync();

        foreach (var emailLog in emailLogs)
        {
            var status = await this.chesClient.GetStatusAsync(emailLog.NotificationId!.Value);
            if (status != null && emailLog.LatestStatus != status)
            {
                emailLog.LatestStatus = status;
            }
            emailLog.UpdateCount++;
        }
        await this.context.SaveChangesAsync();

        return totalCount;
    }



    private static class SendType
    {
        public const string Ches = "CHES";
        public const string Smtp = "SMTP";
    }
}
