﻿using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.HttpClients.Mail;
using NotificationService.Kafka.Interfaces;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using NotificationService.Services;

namespace NotificationService.NotificationEvents.UserProvisioning.Handler;
public class UserProvisioningHandler : IKafkaHandler<string, UserProvisioningModel>
{
    private readonly IKafkaProducer<string, NotificationAckModel> _producer;
    private readonly NotificationServiceConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly NotificationDbContext _context;
    private readonly IChesClient chesClient;

    public UserProvisioningHandler(IKafkaProducer<string, NotificationAckModel> producer, NotificationServiceConfiguration configuration, IEmailService emailService, NotificationDbContext context, IChesClient chesClient)
    {
        _producer = producer;
        _configuration = configuration;
        _emailService = emailService;
        _context = context;
        this.chesClient = chesClient;
    }
    public async Task<Task> HandleAsync(string consumerName, string key, UserProvisioningModel value)
    {
        //check wheather this message has been processed before   
        if (await _context.HasBeenProcessed(key, consumerName))
        {
            return Task.CompletedTask;
        }
        //Send Notification to user
        var msgId = await this.SendConfirmationEmailAsync(value.Email, value.FirstName, value.UserName);
        var emailLogs = await _context.EmailLogs
            .Where(log => log.MsgId == msgId!.Value && log.LatestStatus != ChesStatus.Completed)
            .ToListAsync();
        using (var trx = _context.Database.BeginTransaction())
        {
            //new notification? check message status
            if (emailLogs != null && emailLogs.Count == 1 && emailLogs[0].MsgId!.Value != Guid.Empty)
            {
                var emailLog = emailLogs[0];
                var status = await this.chesClient.GetStatusAsync(emailLog.MsgId!.Value);

                if (status != null && emailLog.LatestStatus != status)
                {
                    emailLog.LatestStatus = status;
                }

                await _context.IdempotentConsumer(messageId: key, consumer: consumerName);

                //save notification ref in notification table database
                await _context.Notifications.AddAsync(new NotificationAckModel
                {
                    PartId = value.ParticipantId,
                    NotificationId = msgId!.Value.ToString(),
                    EmailAddress = value.Email,
                    Status = ChesStatus.Completed,
                    Consumer = consumerName
                });
                await _context.SaveChangesAsync();
                //After successful operation, we can produce message for other service's consumption

                await _producer.ProduceAsync(_configuration.KafkaCluster.AckTopicName, key: msgId!.Value.ToString(), new NotificationAckModel
                {
                    PartId = value.ParticipantId,
                    NotificationId = msgId!.Value.ToString(),
                    EmailAddress = value.Email,
                    Status = ChesStatus.Completed
                });



            }
            await trx.CommitAsync();
        }

        return Task.CompletedTask;
    }
    private async Task<Guid?> SendConfirmationEmailAsync(string partyEmail,string firstName, string username)
    {
        // TODO email text

        string msgBody = string.Format(@"<html>
            <head>
                <title>Justin User Account Provisiong</title>
            </head>
                <body> 
                <img src='https://drive.google.com/uc?export=view&id=16JU6XoVz5FvFUXXWCN10JvN-9EEeuEmr'width='' height='50'/><br/><br/><div style='border-top: 3px solid #22BCE5'><span style = 'font-family: Arial; font-size: 10pt' ><br/> Hello {0},<br/><br/> Your Justin user account has been provisioned.<br/><br/>
                You can Log in to the <a href='{1}'> JIPD Portal </a> with your IDIR <b>{2}</b> to continue your onboarding into the Digital Evidence Management System by clicking on the above link. <br/><br/> Thanks <br/> Justin User Management.
                </span></div></body></html> ", 
    firstName, "https://dev.pidp-e27db1-dev.apps.gold.devops.gov.bc.ca/", username);
        var email = new Email(
            from: EmailService.NotificationServiceEmail,
            to: partyEmail,
            subject: "Your Justin User Account has been Provisoned",
            body: msgBody
        );
      return await _emailService.SendAsync(email);
    }
}

