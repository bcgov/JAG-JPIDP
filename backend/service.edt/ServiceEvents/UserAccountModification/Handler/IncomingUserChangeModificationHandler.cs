namespace edt.service.ServiceEvents.UserAccountModification.Handler;
using edt.service.Data;
using edt.service.Exceptions;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka.Interfaces;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.UserAccountCreation.Models;
using edt.service.ServiceEvents.UserAccountModification.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class IncomingUserChangeModificationHandler : IKafkaHandler<string, IncomingUserModification>
{
    private readonly IKafkaProducer<string, Notification> producer;
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> responseProducer;

    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly EdtDataStoreDbContext context;


    public IncomingUserChangeModificationHandler(
        IKafkaProducer<string, Notification> producer,
        IKafkaProducer<string, GenericProcessStatusResponse> responseProducer,
        IKafkaProducer<string, UserModificationEvent> userModificationProducer,
        EdtServiceConfiguration configuration,
        IEdtClient edtClient,
        EdtDataStoreDbContext context,
        IKafkaProducer<string, EdtUserProvisioningModel> retryProducer, ILogger logger)
    {
        this.producer = producer;
        this.responseProducer = responseProducer;
        this.configuration = configuration;
        this.context = context;
        this.logger = logger;
        this.edtClient = edtClient;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, IncomingUserModification incomingUserModification)
    {

        Serilog.Log.Information($"Message {key} received on topic {consumerName} for {incomingUserModification.UserID} {incomingUserModification.Key}");

        if (await this.context.HasBeenProcessed(key, consumerName))
        {
            return Task.CompletedTask;
        }

        if (incomingUserModification.IdpType == "verified")
        {
            // we'll only permit email changes for verified credentials users (TBD how we handle name changes)
            await this.edtClient.ModifyPerson(incomingUserModification);
        }
        else
        {
            return await this.HandleBCPSUserChange(consumerName, key, incomingUserModification);
        }

        await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
        await this.context.SaveChangesAsync();

        return Task.CompletedTask;

    }


    private async Task<Task> HandleBCPSUserChange(string consumerName, string key, IncomingUserModification incomingUserModification)
    {
        var userInfo = await this.edtClient.GetUser(incomingUserModification.Key);

        if (userInfo == null)
        {
            Serilog.Log.Error($"Failed to find EDT user with key {incomingUserModification.Key} for update");
            return Task.FromException(new EdtServiceException("$Failed to find EDT user with key {incomingUserModification.Key} for update"));
        }

        var userModificationEvent = new UserModificationEvent
        {
            eventTime = DateTime.Now,
            partId = incomingUserModification.Key,
            eventType = UserModificationEvent.UserEvent.Modify
        };

        var domainEvent = "digitalevidence-bcps-userupdate-complete";
        var eventData = new Dictionary<string, string>
        {
            { "firstName", userInfo.FullName.Split(" ").FirstOrDefault() }
        };



        // if account disabled then ignore everything else
        if (incomingUserModification.IsAccountDeactivated())
        {

            domainEvent = "digitalevidence-bcps-userupdate-deactivated";
            Serilog.Log.Information($"Deactivating account for {incomingUserModification.Key}");

            var disabledOk = await this.edtClient.DisableAccount(incomingUserModification.Key);

            if (disabledOk)
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

            if (incomingUserModification.IsAccountActivated())
            {
                var responseOk = await this.edtClient.EnableAccount(incomingUserModification.Key);
                Serilog.Log.Information($"Account for participant {incomingUserModification.Key} has been activated");
                userModificationEvent.eventType = UserModificationEvent.UserEvent.Enable;
            }

            var fullName = userInfo.FullName.Split(" ");
            var nameChange = false;

            if (incomingUserModification.SingleChangeTypes.ContainsKey(ChangeType.FIRSTNAME))
            {
                Serilog.Log.Information($"First name change detected for {incomingUserModification.Key} - From [{incomingUserModification.SingleChangeTypes[ChangeType.FIRSTNAME].From}] to [{incomingUserModification.SingleChangeTypes[ChangeType.FIRSTNAME].To}]");
                fullName[0] = incomingUserModification.SingleChangeTypes[ChangeType.FIRSTNAME].To;
                nameChange = true;

            }
            if (incomingUserModification.SingleChangeTypes.ContainsKey(ChangeType.LASTNAME))
            {
                Serilog.Log.Information($"Last name change detected for {incomingUserModification.Key} - From [{incomingUserModification.SingleChangeTypes[ChangeType.LASTNAME].From}] to [{incomingUserModification.SingleChangeTypes[ChangeType.LASTNAME].To}]");
                fullName[fullName.Length - 1] = incomingUserModification.SingleChangeTypes[ChangeType.LASTNAME].To;
                nameChange = true;
            }

            if (nameChange)
            {
                userInfo.FullName = string.Join(" ", fullName);
                var result = await this.edtClient.UpdateUserDetails(userInfo);
            }

            // handle regional assignment changes
            if (incomingUserModification.ListChangeTypes.ContainsKey(ChangeType.REGIONS))
            {
                Serilog.Log.Information($"Region change detected for {incomingUserModification.Key} - From [{string.Join(",", incomingUserModification.ListChangeTypes[ChangeType.REGIONS].From)}] to [{string.Join(",", incomingUserModification.ListChangeTypes[ChangeType.REGIONS].To)}]");

                var regionChanges = incomingUserModification.ListChangeTypes[ChangeType.REGIONS];
                var newRegions = regionChanges.To.Except(regionChanges.From).ToList();
                var removedRegions = regionChanges.From.Except(regionChanges.To).ToList();

                var changesMade = await this.edtClient.UpdateUserAssignedGroups(incomingUserModification.Key, newRegions, removedRegions);


            }
        }

        if (domainEvent == "digitalevidence-bcps-userupdate-complete")
        {
            eventData.Add("changeList", incomingUserModification.ToChangeHtml());
        }


        // publish a notification event and a user modification event
        await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, Guid.NewGuid().ToString(), new Notification
        {
            DomainEvent = domainEvent,
            To = incomingUserModification.UserID,
            EventData = eventData
        });

        // send a response that the process is complete
        var sentStatus = this.responseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
        {
            DomainEvent = "digitalevidence-bcps-edt-userupdate-complete",
            Id = incomingUserModification.ChangeId,
            EventTime = SystemClock.Instance.GetCurrentInstant(),
            Status = "Complete",
            TraceId = incomingUserModification.Key
        });


        return Task.CompletedTask;
    }

    public Task<Task> HandleRetryAsync(string consumerName, string key, IncomingUserModification value, int retryCount, string topicName)
    {
        throw new NotImplementedException("Currently not implemented");
    }
}
