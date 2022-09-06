﻿using Microsoft.EntityFrameworkCore;
using NodaTime;
using NotificationService.Data;
using NotificationService.HttpClients.Mail;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using System.Linq.Expressions;

namespace NotificationService.Services;

public class EmailService : IEmailService
{
    public const string NotificationServiceEmail = "justinuserprovisioning@gov.bc.ca";

    private readonly IChesClient chesClient;
    private readonly IClock clock;
    private readonly ILogger logger;
    private readonly ISmtpEmailClient smtpEmailClient;
    private readonly NotificationServiceConfiguration config;
    private readonly NotificationDbContext context;

    public EmailService(
        IChesClient chesClient,
        IClock clock,
        ILogger<EmailService> logger,
        ISmtpEmailClient smtpEmailClient,
        NotificationServiceConfiguration config,
        NotificationDbContext context)
    {
        this.chesClient = chesClient;
        this.clock = clock;
        this.logger = logger;
        this.smtpEmailClient = smtpEmailClient;
        this.config = config;
        this.context = context;
        this.context = context;
    }

    public async Task<Guid?> SendAsync(Email email)
    {
        if (!NotificationServiceConfiguration.IsProduction())
        {
            email.Subject = $"{email.Subject}";
        }

        if (this.config.ChesClient.Enabled && await this.chesClient.HealthCheckAsync())
        {
            var msgId = await this.chesClient.SendAsync(email);
            await this.CreateEmailLog(email, SendType.Ches, msgId);

            if (msgId != null)
            {
                return msgId;
            }
        }

        // Fall back to SMTP client
        //await this.CreateEmailLog(email, SendType.Smtp);
        //await this.smtpEmailClient.SendAsync(email);
        return Guid.Empty;
    }

    private async Task CreateEmailLog(Email email, string sendType, Guid? msgId = null)
    {
        this.context.EmailLogs.Add(new EmailLog(email, sendType, msgId, this.clock.GetCurrentInstant()));
        await this.context.SaveChangesAsync();
    }
    public async Task<int> UpdateEmailLogStatuses(int limit)
    {
        Expression<Func<EmailLog, bool>> predicate = log =>
            log.SendType == SendType.Ches
            && log.MsgId != null
            && log.LatestStatus != ChesStatus.Completed;

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
            var status = await this.chesClient.GetStatusAsync(emailLog.MsgId!.Value);
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