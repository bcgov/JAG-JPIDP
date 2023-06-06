namespace edt.disclosure.ServiceEvents.UserAccountCreation.Handler;

using System.Diagnostics;
using edt.disclosure.Data;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.HttpClients.Services.EdtDIsclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Kafka.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus;

public class UserProvisioningHandler : IKafkaHandler<string, EdtDisclosureUserProvisioningModel>
{


    private readonly EdtDisclosureServiceConfiguration configuration;
    private readonly IEdtDisclosureClient edtClient;
    private readonly ILogger logger;
    private readonly DisclosureDataStoreDbContext context;
    private static readonly Counter TombstoneConversions = Metrics.CreateCounter("edt_tombstone_conversions", "Number of tombstone accounts activated");


    public UserProvisioningHandler(
        EdtDisclosureServiceConfiguration configuration,
        IEdtDisclosureClient edtClient,
        DisclosureDataStoreDbContext context)
    {

        this.configuration = configuration;
        this.context = context;
        this.logger = logger;
        this.edtClient = edtClient;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, EdtDisclosureUserProvisioningModel accessRequestModel)
    {

        // check this message is for us

        if (accessRequestModel.SystemName != null && !(accessRequestModel.SystemName.Equals("DISCLOSURE", StringComparison.Ordinal)))
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
            //if (await this.context.HasBeenProcessed(key, consumerName))
            //{
            //    //await trx.RollbackAsync();
            //    return Task.CompletedTask;
            //}
            ///check whether edt service api is available before making any http request
            ///
            /// call version endpoint via get
            ///
            var edtVersion = await this.CheckEdtServiceVersion();



            //check whether edt user already exist
            var result = await this.AddOrUpdateUser(accessRequestModel);


            if (result.successful)
            {
                //add to tell message has been processed by consumer
                //await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);

                await this.context.SaveChangesAsync();

   

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


                        //await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
                        //{
                        //    To = accessRequestModel.Email,
                        //    DomainEvent = "digitalevidence-sa-usercreation-complete",
                        //    EventData = eventData,
                        //});

                        //// we'll flag it as completed-provisioning as the account is really not fully complete
                        //// at this stage. Once the ISL service sends us a message that the user also has all cases assigned then we'll send a final
                        //// email stating such to the user
                        //await this.ackProducer.ProduceAsync(this.configuration.KafkaCluster.AckTopicName, key: key, new NotificationAckModel
                        //{
                        //    Subject = NotificationSubject.AccessRequest,
                        //    AccessRequestId = accessRequestModel.AccessRequestId,
                        //    Status = "Completed"
                        //});

                        await trx.CommitAsync();
                        Serilog.Log.Information($"User {result.partId} provision event published to {this.configuration.KafkaCluster.AckTopicName}");

                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Logger.Error($"Failed to publish to user notification topic - rolling back transaction [{string.Join(",", ex.Message)}");
                        await trx.RollbackAsync();
                    }
                }
               
            
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error("Exception during EDT Disclosure provisioning {0}", ex.Message);
  
        }

        return Task.CompletedTask; //create specific exception handler later
    }


    private async Task<string> CheckEdtServiceVersion() => await this.edtClient.GetVersion();

    private async Task<UserModificationEvent> AddOrUpdateUser(EdtDisclosureUserProvisioningModel value)
    {
        var user = await this.edtClient.GetUser(value.Key!);


            //create user account in EDT
            var result = user == null
                ? await this.edtClient.CreateUser(value)
                : await this.edtClient.UpdateUser(value, user);
            return result;



    }

   

    private async Task NotifyUserFailure(EdtDisclosureUserProvisioningModel value, string key, string topic)
    {
        var msgId = Guid.NewGuid().ToString();

        var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", value.FullName!.Split(' ').FirstOrDefault("NAME_NOT_SET") },
                        { "PartyId", value.Key! },
                        { "Tag", msgId! }
                    };

        //await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProducerTopicName, key: key, new Notification
        //{
        //    To = value.Email,
        //    DomainEvent = "digitalevidence-disclosure-usercreation-failure",
        //    EventData = eventData,
        //    Subject = "Digital Evidence Management System Notification",
        //});
    }


}
public static partial class UserProvisioningHandlerLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Cannot provision disclosure user with partId {partId} and request Id {accessrequestId}. Published event key {accessrequestId} of {fromTopic} record to {topic} topic for retrial")]
    public static partial void LogUserAccessPublishError(this ILogger logger, string? partId, string accessrequestId, string fromTopic, string topic);
    [LoggerMessage(2, LogLevel.Error, "Error creating or updating edt disclosure user with partId {partId} and access requestId {accessRequestId} after final retry")]
    public static partial void LogUserAccessRetryError(this ILogger logger, string partId, string accessRequestId);
}

