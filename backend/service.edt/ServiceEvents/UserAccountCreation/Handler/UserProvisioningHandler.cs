namespace edt.service.ServiceEvents.UserAccountCreation.Handler;

using System.Diagnostics;
using edt.service.Data;
using edt.service.Exceptions;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka;
using edt.service.Kafka.Interfaces;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.UserAccountCreation.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus;
using static edt.service.EdtServiceConfiguration;

public class UserProvisioningHandler : IKafkaHandler<string, EdtUserProvisioningModel>
{
    private readonly IKafkaProducer<string, Notification> producer;
    private readonly IKafkaProducer<string, NotificationAckModel> ackProducer;

    private readonly IKafkaProducer<string, EdtUserProvisioningModel> retryProducer;
    private readonly IKafkaProducer<string, UserModificationEvent> userModificationProducer;

    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly EdtDataStoreDbContext context;
    private static readonly Counter TombstoneConversions = Metrics.CreateCounter("edt_tombstone_conversions", "Number of tombstone accounts activated");


    public UserProvisioningHandler(
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

    public async Task<Task> HandleAsync(string consumerName, string key, EdtUserProvisioningModel accessRequestModel)
    {

        // check this message is for us

        if (accessRequestModel.SystemName != null && !(accessRequestModel.SystemName.Equals("DEMS", StringComparison.Ordinal) || accessRequestModel.SystemName.Equals("DigitalEvidence", StringComparison.Ordinal) || accessRequestModel.SystemName.Equals("DigitalEvidenceCaseManagement", StringComparison.Ordinal)))
        {
            Serilog.Log.Logger.Information($"Ignoring message {key} for system {accessRequestModel.SystemName} as we only handle DEMS requests");
            return Task.CompletedTask;
        }

        var plainTextTopic = Environment.GetEnvironmentVariable("USER_CREATION_PLAINTEXT_TOPIC");


        // set activity info
        Activity.Current?.AddTag("digitalevidence.access.id", accessRequestModel.AccessRequestId);


        using var trx = this.context.Database.BeginTransaction();
        try
        {
            //check whether this message has been processed before   
            if (await this.context.HasBeenProcessed(key, consumerName))
            {
                //await trx.RollbackAsync();
                return Task.CompletedTask;
            }
            ///check whether edt service api is available before making any http request
            ///
            /// call version endpoint via get
            ///
            var edtVersion = await this.CheckEdtServiceVersion();



            //check whether edt user already exist
            var result = await this.AddOrUpdateUser(accessRequestModel);


            if (result.successful)
            {
                //add to tell message has been proccessed by consumer
                await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);

                await this.context.SaveChangesAsync();

                // submitting agency users(e.g. police) that have limited DEMS access
                if (result.submittingAgencyUser)
                {
                    Serilog.Log.Information($"User {result.partId} was for submitting agency - publishing event change only");

                    try
                    {

                        // create event data
                        var eventData = new Dictionary<string, string>
                        {
                        { "FirstName", accessRequestModel.FullName!.Split(' ').FirstOrDefault("NAME_NOT_SET") },
                        { "Organization", accessRequestModel.OrganizationName! },
                        { "PartId", "" + result.partId },
                        { "AccessRequestId", "" + accessRequestModel.AccessRequestId },
                        { "MessageId", key! }
                         };


                        await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
                        {
                            To = accessRequestModel.Email,
                            DomainEvent = "digitalevidence-sa-usercreation-complete",
                            EventData = eventData,
                        });

                        // we'll flag it as completed-provisioning as the account is really not fully complete
                        // at this stage. Once the ISL service sends us a message that the user also has all cases assigned then we'll send a final
                        // email stating such to the user
                        await this.ackProducer.ProduceAsync(this.configuration.KafkaCluster.AckTopicName, key: key, new NotificationAckModel
                        {
                            Subject = NotificationSubject.AccessRequest,
                            AccessRequestId = accessRequestModel.AccessRequestId,
                            Status = "Completed"
                        });

                        await trx.CommitAsync();
                        Serilog.Log.Information($"User {result.partId} provision event published to {this.configuration.KafkaCluster.AckTopicName}");

                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Logger.Error($"Failed to publish to user notification topic - rolling back transaction [{string.Join(",", ex.Message)}");
                        await trx.RollbackAsync();
                    }
                }
                // non submitting agency users
                else
                {

                    var msgKey = Guid.NewGuid().ToString();

                    // create event data
                    var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequestModel.FullName!.Split(' ').FirstOrDefault("NAME_NOT_SET") },
                        { "PartyId", accessRequestModel.Key! },
                        { "AccessRequestId", "" + accessRequestModel.AccessRequestId },
                        { "Tag", msgKey! }
                    };

                    var domainEvent = result.eventType == UserModificationEvent.UserEvent.EnableTombstone ? "digitalevidence-bcps-usercreation-tombstone-complete" : "digitalevidence-bcps-usercreation-complete";

                    await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
                    {
                        To = accessRequestModel.Email,
                        DomainEvent = domainEvent,

                        EventData = eventData,
                    });

                    if (string.IsNullOrEmpty(this.configuration.SchemaRegistry.Url))
                    {
                        throw new EdtServiceException("Schema registry is not configured");
                    }

                    var producer = new SchemaAwareProducer(ConsumerSetup.GetProducerConfig(), this.userModificationProducer, this.configuration);

                    // publish to the user creation topic for others to consume
                    bool publishResultOk;
                    if (result.eventType == UserModificationEvent.UserEvent.Create)
                    {
                        Serilog.Log.Information("Publishing EDT user creation event {0} {1}", msgKey, accessRequestModel.Key);
                        if (!string.IsNullOrEmpty(plainTextTopic))
                        {
                            Serilog.Log.Information("Publishing EDT user creation event to secondary topic", msgKey, accessRequestModel.Key);
                            await this.userModificationProducer.ProduceAsync(plainTextTopic, key: msgKey, result);
                        }
                        publishResultOk = await producer.ProduceAsync(this.configuration.KafkaCluster.UserCreationTopicName, key: msgKey, result);
                    }
                    else
                    {
                        Serilog.Log.Information("Publishing EDT user modification event {0} {1}", msgKey, accessRequestModel.Key);
                        publishResultOk = await producer.ProduceAsync(this.configuration.KafkaCluster.UserModificationTopicName, key: msgKey, result);
                    }


                    if (publishResultOk)
                    {
                        await trx.CommitAsync();
                    }
                    else
                    {
                        Serilog.Log.Logger.Error("Failed to publish to user notification topic - rolling back transaction");
                        await trx.RollbackAsync();
                    }

                    return Task.FromResult(publishResultOk);

                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error("Exception during EDT provisioning {0}", ex.Message);
            //on exception rollback trx, publish to retry topic for retry and commit offset 
            //await trx.RollbackAsync();
            // publish to the initial error topic
            var initialRetryTopic = this.configuration.RetryPolicy.RetryTopics.Find(retryTopic => retryTopic.Order == 1);
            if (initialRetryTopic == null)
            {
                throw new EdtServiceException("Unable to locate retry topic with Order=1");
            }
            else
            {
                Serilog.Log.Information("Adding retry entry to {0}", initialRetryTopic.TopicName);
                await this.PublishToRetryTopic(accessRequestModel, key, initialRetryTopic);
            }
            // this.logger.LogUserAccessPublishError(accessRequestModel.Key, key, this.configuration.KafkaCluster.ProducerTopicName, this.configuration.KafkaCluster.InitialRetryTopicName);
        }

        return Task.CompletedTask; //create specific exception handler later
    }


    private async Task<string> CheckEdtServiceVersion() => await this.edtClient.GetVersion();

    private async Task<UserModificationEvent> AddOrUpdateUser(EdtUserProvisioningModel value)
    {
        var user = await this.edtClient.GetUser(value.Key!);

        // determine if this user is a tombstone account (see BCPSDEMS-1033)
        // tombstone accounts are created in advance of the user on-boarding such that as cases are added to EDT they are
        // already available to the user. This alleviates the lengthy case assignment process that is required for new JUSTIN users accessing EDT
        if (user != null && !string.IsNullOrEmpty(this.configuration.EdtClient.TombStoneEmailDomain) && (user.Email.EndsWith(this.configuration.EdtClient.TombStoneEmailDomain, StringComparison.OrdinalIgnoreCase) && user.IsActive == false))
        {
            this.logger.LogUserTombstoneActivation(value.Key, value.AccessRequestId);
            var response = await this.edtClient.EnableTombstoneAccount(value, user);
            if (response.successful)
            {
                TombstoneConversions.Inc();
            }

            return response;
        }
        else
        {

            //create user account in EDT
            var result = user == null
                ? await this.edtClient.CreateUser(value)
                : await this.edtClient.UpdateUser(value, user, false);
            return result;
        }


    }

    /// <summary>
    /// Publish to the retry topic
    /// </summary>
    /// <param name="value"></param>
    /// <param name="key"></param>
    /// <param name="retryTopicModel"></param>
    /// <returns></returns>
    private async Task PublishToRetryTopic(EdtUserProvisioningModel value, string key, RetryTopicModel retryTopicModel)
    {
        Serilog.Log.Information("Publishing to retry topic {0} {1}", value.Key, retryTopicModel.TopicName);
        // different topic then reset to 1, otherwise increment retry count

        value.RetryNumber = retryTopicModel.Order == value.TopicOrder ? value.RetryNumber : 1;
        value.RetryDuration = TimeSpan.FromMinutes(retryTopicModel.DelayMinutes);
        value.TopicOrder = retryTopicModel.Order;
        if (retryTopicModel.RetryCount >= value.RetryNumber)
        {
            // place onto the topic 
            await this.retryProducer.ProduceAsync(retryTopicModel.TopicName, key, value);

            var msgId = Guid.NewGuid().ToString();

            // if notification set and first retry attempt then send a message
            // we only want to notify once per retry topic unless NotifyOnEachRetry is set
            if (retryTopicModel.NotifyUser && (value.RetryNumber == 1 || retryTopicModel.NotifyOnEachRetry))
            {
                Serilog.Log.Information("Sending email to user to notify of retry");


                var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", value.FullName!.Split(' ').FirstOrDefault("NAME_NOT_SET") },
                        { "PartyId", value.Key! },
                        { "Tag", msgId! }
                    };

                await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
                {
                    To = value.Email,
                    DomainEvent = "digitalevidence-bcps-usercreation-retry",
                    EventData = eventData,
                    Subject = "Digital Evidence Management System Notification",
                });


            }
        }
    }

    private async Task PublishToDeadLetterTopic(EdtUserProvisioningModel value, string key, string deadLetterTopic)
    {
        Serilog.Log.Information("Publishing to dead letter topic {0} {1}", value.Key);
        await this.retryProducer.ProduceAsync(deadLetterTopic, key, value);
    }

    private async Task NotifyUserFailure(EdtUserProvisioningModel value, string key, string topic)
    {
        var msgId = Guid.NewGuid().ToString();

        var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", value.FullName!.Split(' ').FirstOrDefault("NAME_NOT_SET") },
                        { "PartyId", value.Key! },
                        { "Tag", msgId! }
                    };

        await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
        {
            To = value.Email,
            DomainEvent = "digitalevidence-bcps-usercreation-failure",
            EventData = eventData,
            Subject = "Digital Evidence Management System Notification",
        });
    }


    public async Task<Task> HandleRetryAsync(string consumerName, string key, EdtUserProvisioningModel value, int retryCount, string topicName)
    {
        using var trx = this.context.Database.BeginTransaction();

        try
        {

            //check wheather this message has been processed before   
            if (await this.context.HasBeenProcessed(key, consumerName))
            {
                await trx.RollbackAsync();
                return Task.CompletedTask;
            }
            ///check weather edt service api is available before making any http request
            ///
            /// call version endpoint via get
            ///
            var edtVersion = await this.CheckEdtServiceVersion();

            if (edtVersion == null)
            {
                await trx.RollbackAsync();
                Serilog.Log.Logger.Error("Failed to ping EDT service");
                return Task.FromException(new EdtServiceException("Unable to access EDT endpoint"));
            }

            //check whether edt user already exist
            var result = await this.AddOrUpdateUser(value);

            //// TODO REMOVE THIS BLOCK ONCE TESTED
            //Serilog.Log.Error("THROWING FAKE EXCEPTION!!!!!!!");
            //throw new EdtServiceException("FAKE EXCEPTION!!!");
            //// TODO REMOVE THIS BLOCK ONCE TESTED


            if (result.successful)
            {
                //add to tell message has been proccessed by consumer
                await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);

                await this.context.SaveChangesAsync();
                //After successful operation, we can produce message for other service's consumption e.g. Notification service
                var msgId = Guid.NewGuid().ToString();

                // create event data
                var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", value.FullName!.Split(' ').FirstOrDefault("NAME_NOT_SET") },
                        { "PartyId", value.Key! },
                        { "AccessRequestId", "" + value.AccessRequestId }
                    };

                await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
                {
                    To = value.Email,
                    DomainEvent = "digitalevidence-bcps-usercreation-complete",
                    EventData = eventData,
                });


                await trx.CommitAsync();

                return Task.CompletedTask;

            }

        }
        catch (Exception e)
        {

            await trx.RollbackAsync();
            // get the last retry number
            var currentTopic = this.configuration.RetryPolicy.RetryTopics.Find(retryTopic => retryTopic.Order == value.TopicOrder);

            if (currentTopic == null)
            {
                throw new EdtServiceException($"Did not find a topic with order number {value.TopicOrder}");
            }


            if (value.RetryNumber >= currentTopic.RetryCount)
            {

                // skip to the next retry topic - if no topics left then send to dead letter topic and inform user of failure
                var nextRetryModel = this.configuration.RetryPolicy.RetryTopics.Find(retryTopic => retryTopic.Order == value.TopicOrder + 1);

                if (nextRetryModel == null)
                {
                    Serilog.Log.Warning("No more retry topics found - sending to dead letter topic");
                    await this.context.FailedEventLogs.AddAsync(new FailedEventLog
                    {
                        EventId = Guid.NewGuid().ToString(),
                        Producer = this.configuration.KafkaCluster.ProducerTopicName,
                        ConsumerGroupId = this.configuration.KafkaCluster.RetryConsumerGroupId,
                        ConsumerId = consumerName,
                        EventPayload = value
                    });

                    // publish to dead letter topic
                    await this.PublishToDeadLetterTopic(value, key, this.configuration.RetryPolicy.DeadLetterTopic);


                    // notify user
                    await this.NotifyUserFailure(value, key, this.configuration.KafkaCluster.ProducerTopicName);

                    await this.context.SaveChangesAsync();
                    this.logger.LogUserAccessRetryError(value.Key!, key);

                    // we didnt complete the request but we want the offset committed so we
                    // dont continue to process the messages
                    return Task.CompletedTask;
                }
                else
                {
                    Serilog.Log.Information("Moving to next retry topic");
                    await this.PublishToRetryTopic(value, key, nextRetryModel);
                }
            }
            else
            {
                // increase the retryCount and publish to same topic (if the timeout has been reached)
                var retryMessage = value;
                retryMessage.RetryNumber++;
                Serilog.Log.Information("Resending message to retry topic");
                await this.PublishToRetryTopic(retryMessage, key, currentTopic);
            }

        }
        return Task.CompletedTask;

    }
}
public static partial class UserProvisioningHandlerLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Cannot provisioned user with partId {partId} and request Id {accessrequestId}. Published event key {accessrequestId} of {fromTopic} record to {topic} topic for retrial")]
    public static partial void LogUserAccessPublishError(this ILogger logger, string? partId, string accessrequestId, string fromTopic, string topic);
    [LoggerMessage(2, LogLevel.Error, "Error creating or updating edt user with partId {partId} and access requestId {accessRequestId} after final retry")]
    public static partial void LogUserAccessRetryError(this ILogger logger, string partId, string accessRequestId);
    [LoggerMessage(3, LogLevel.Information, "User account for partId {partId} is a tombstone account and will be activated at this time ReqId: {accessRequestId}")]
    public static partial void LogUserTombstoneActivation(this ILogger logger, string partId, int accessRequestId);
}

