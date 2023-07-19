namespace NotificationService.NotificationEvents.UserProvisioning.Handler;

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NotificationService.Data;
using NotificationService.Exceptions;
using NotificationService.HttpClients.Mail;
using NotificationService.Kafka.Interfaces;
using NotificationService.Models;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using NotificationService.Services;
using Prometheus;

public class UserProvisioningHandler : IKafkaHandler<string, Notification>
{
    private readonly IKafkaProducer<string, NotificationAckModel> producer;
    private readonly NotificationServiceConfiguration configuration;
    private readonly IEmailService emailService;
    private readonly NotificationDbContext context;
    private readonly IChesClient chesClient;

    private static readonly Counter consumeCount = Metrics.CreateCounter("jum_notify_consume_count", "Number of notification messages consumed");
    private static readonly Counter duplicateConsumeCount = Metrics.CreateCounter("jum_notify_dup_consume_count", "Number of duplicated notification messages consumed");

    public UserProvisioningHandler(NotificationServiceConfiguration configuration, IKafkaProducer<string, NotificationAckModel> producer, IEmailService emailService, NotificationDbContext context, IChesClient chesClient)
    {
        this.producer = producer;
        this.configuration = configuration;
        this.emailService = emailService;
        this.context = context;
        this.chesClient = chesClient;
    }
    public async Task<Task> HandleAsync(string consumerName, string key, Notification value)
    {
        //check whether this message has been processed before
        Guid? sendResponseId = Guid.Empty;
        consumeCount.Inc();

        // notification id is the message topic key
        value.NotificationId = Guid.Parse(key);

        Serilog.Log.Information($"Checking if message {key} has already been processed by {consumerName}");

        if (await this.context.HasBeenProcessed(key, consumerName))
        {
            Serilog.Log.Information($"Message {key} has already been processed");
            duplicateConsumeCount.Inc();
            return Task.CompletedTask;
        }

        //check whether the message tag has already been processed via ches
        if (await this.context.EmailLogs.AnyAsync(emailLog => emailLog.NotificationId == value.NotificationId && emailLog.LatestStatus == ChesStatus.Complete))
        {
            return Task.CompletedTask;
        }


        if (!await this.context.EmailLogs.AnyAsync(emailLog => emailLog.NotificationId == value.NotificationId))
        {
            sendResponseId = await this.SendConfirmationEmailAsync(value);
        }

        // email will be flagged as accepted once submitted to CHES
        var emailLogs = await this.context.EmailLogs
             .Where(emailLog => emailLog.NotificationId.Equals(value.NotificationId) && emailLog.LatestStatus == ChesStatus.Accepted)
             .ToListAsync();

        using var trx = this.context.Database.BeginTransaction();
        try
        {
            //new notification? check message status
            if (emailLogs != null && emailLogs.Count == 1 && emailLogs.FirstOrDefault()!.NotificationId!.Value != Guid.Empty)
            {
                var emailLog = emailLogs.FirstOrDefault();
                var status = await this.chesClient.GetStatusAsync(emailLog!.SentResponseId!.Value);

                if (status != null && emailLog.LatestStatus != status)
                {
                    emailLog.LatestStatus = status;
                }

                await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);

                var existingNotification = this.context.Notifications.Where(notification => notification.NotificationId == value.NotificationId).FirstOrDefault();

                if (existingNotification != null)
                {
                    Serilog.Log.Information($"Notification already exists for {value.NotificationId} - {value.To}");

                }
                else
                {
                    //save notification ref in notification table database
                    await this.context.Notifications.AddAsync(new NotificationAckModel
                    {
                        PartId = value.EventData.ContainsKey("partyId") ? value.EventData["partyId"] : "",
                        NotificationId = value.NotificationId,
                        DomainEvent = value.DomainEvent,
                        EmailAddress = value.To!,
                        Status = "complete",
                        EventData = value.EventData != null ? JsonConvert.SerializeObject(value.EventData) : "",
                        Consumer = consumerName,
                        AccessRequestId = value.EventData.ContainsKey("accessRequestId") ? Convert.ToInt32(value.EventData["accessRequestId"]) : -1
                    });
                }

                await this.context.SaveChangesAsync();

                var partId = value.EventData.ContainsKey("partId") ? value.EventData["partId"] : value.EventData.ContainsKey("partyId") ? value.EventData["partyId"] : "";

                //After successful operation, we can produce message for other service's consumption
                // if its a non-tombstone account then we'll set the status to completed-pending-finalization
                var ackStatus = value.DomainEvent.Equals("digitalevidence-bcps-usercreation-complete") ? "Completed-Pending-Case-Allocation" : "complete";
                await this.producer.ProduceAsync(this.configuration.KafkaCluster.AckTopicName, key: value.NotificationId.ToString()!, new NotificationAckModel
                {
                    PartId = partId,
                    NotificationId = value.NotificationId,
                    EmailAddress = value.To!,
                    Status = ackStatus,
                    DomainEvent = value.DomainEvent,
                    AccessRequestId = value.EventData.ContainsKey("accessRequestId") ? Convert.ToInt32(value.EventData["accessRequestId"]) : -1
                });


                await trx.CommitAsync();

                return Task.CompletedTask;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Failed to update emaillog and store notification {string.Join(",", ex.Message)}");
            await trx.RollbackAsync();
            return Task.FromException(new NotificationException(string.Join(",", ex.Message)));
        }

        return Task.CompletedTask;
    }
    private async Task<Guid?> SendConfirmationEmailAsync(Notification model)
    {
      return await emailService.SendAsync(model);
    }
}

