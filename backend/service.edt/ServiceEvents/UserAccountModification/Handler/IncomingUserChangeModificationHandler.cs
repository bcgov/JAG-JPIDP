namespace edt.service.ServiceEvents.UserAccountModification.Handler;

using System.Diagnostics.Eventing.Reader;
using System.Threading.Channels;
using edt.service.Data;
using edt.service.Exceptions;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka.Interfaces;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.UserAccountCreation.Models;
using edt.service.ServiceEvents.UserAccountModification.Models;
using Microsoft.EntityFrameworkCore;

public class IncomingUserChangeModificationHandler : IKafkaHandler<string, IncomingUserModification>
{
    private readonly IKafkaProducer<string, Notification> producer;
    private readonly IKafkaProducer<string, NotificationAckModel> ackProducer;

    private readonly IKafkaProducer<string, EdtUserProvisioningModel> retryProducer;
    private readonly IKafkaProducer<string, UserModificationEvent> userModificationProducer;

    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly EdtDataStoreDbContext context;



    public IncomingUserChangeModificationHandler(
        IKafkaProducer<string, Notification> producer,
          IKafkaProducer<string, NotificationAckModel> ackProducer,

    IKafkaProducer<string, UserModificationEvent> userModificationProducer,
        EdtServiceConfiguration configuration,
        IEdtClient edtClient,
        EdtDataStoreDbContext context,
        IKafkaProducer<string, EdtUserProvisioningModel> retryProducer, ILogger logger)
    {
        this.producer = producer;
        this.ackProducer = ackProducer;
        this.userModificationProducer = userModificationProducer;
        this.configuration = configuration;
        this.context = context;
        this.logger = logger;
        this.edtClient = edtClient;
        this.retryProducer = retryProducer;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, IncomingUserModification incomingUserModification)
    {

        Serilog.Log.Information($"Message {key} received on topic {consumerName} for {incomingUserModification.UserID} {incomingUserModification.Key}");

        var userModificationEvent = new UserModificationEvent
        {
            eventTime = DateTime.Now,
            partId = incomingUserModification.Key,
            eventType = UserModificationEvent.UserEvent.Modify
        };

        var domainEvent = "digitalevidence-bcps-userupdate-complete";

        // if account disabled then ignore everything else
        if (incomingUserModification.IsAccountDeactivated())
        {
            Serilog.Log.Information($"Deactiviating account for {incomingUserModification.Key}");

            var responseOk = await this.edtClient.DisableAccount(incomingUserModification.Key);

            if (responseOk)
            {
                Serilog.Log.Information($"Account deactivated for user {incomingUserModification.Key}");
                userModificationEvent.eventType = UserModificationEvent.UserEvent.Disable;

            }
            else
            {
                Serilog.Log.Error($"Account deactivated failed for user {incomingUserModification.Key}");
                return Task.FromException(new EdtServiceException($"Failed to disable user account for {incomingUserModification.Key}"));
            }

        }
        else
        {

            if (incomingUserModification.SingleChangeTypes.ContainsKey(ChangeType.ACTIVATION))
            {
                // must be an activation request
                var responseOk = await this.edtClient.EnableAccount(incomingUserModification.Key);
                Serilog.Log.Information($"Account for participant {incomingUserModification.Key} has been activated");
                userModificationEvent.eventType = UserModificationEvent.UserEvent.Enable;
            }

            // handle regional assignment changes
            if (incomingUserModification.ListChangeTypes.ContainsKey(ChangeType.REGIONS))
            {
                Serilog.Log.Information($"Region change detected for {incomingUserModification.Key} - From [{string.Join(",", incomingUserModification.ListChangeTypes[ChangeType.REGIONS].From)}] to [{string.Join(",", incomingUserModification.ListChangeTypes[ChangeType.REGIONS].To)}]");

                var regionChanges = incomingUserModification.ListChangeTypes[ChangeType.REGIONS];
                var newRegions = regionChanges.To.Except(regionChanges.From).ToList();
                var removedRegions = regionChanges.From.Except(regionChanges.To).ToList();

                await this.edtClient.UpdateUserAssignedGroups(incomingUserModification.Key, newRegions, removedRegions);
            }
        }

        var eventData = new Dictionary<string, string>();

        // publish a notification event and a user modification event
        await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, Guid.NewGuid().ToString(), new Notification
        {
            DomainEvent = domainEvent,
            To = incomingUserModification.UserID,
            EventData = eventData
        });


        return Task.CompletedTask;


    }

    public Task<Task> HandleRetryAsync(string consumerName, string key, IncomingUserModification value, int retryCount, string topicName)
    {
        throw new NotImplementedException("Currently not implemented");
    }
}
