namespace edt.disclosure.ServiceEvents.UserAccountCreation.Handler;

using System.Diagnostics;
using AutoMapper;
using Common.Models.EDT;
using edt.disclosure.Data;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Kafka.Model;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.UserAccountCreation.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

public class DefenceUserProvisioningHandler : BaseProvisioningHandler,
    IKafkaHandler<string, EdtDisclosureDefenceUserProvisioningModel>
{


    private readonly EdtDisclosureServiceConfiguration configuration;
    private readonly IEdtDisclosureClient edtClient;
    private readonly IMapper mapper;

    private readonly IClock clock;
    private readonly ILogger logger;
    private readonly DisclosureDataStoreDbContext context;
    private readonly IKafkaProducer<string, Notification> producer;
    private readonly IKafkaProducer<string, PersonFolioLinkageModel> folioLinkageProducer;

    private readonly IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer;

    public DefenceUserProvisioningHandler(
        EdtDisclosureServiceConfiguration configuration,
        IEdtDisclosureClient edtClient,
        IClock clock,
        IMapper mapper,
        ILogger logger,
        IKafkaProducer<string, Notification> producer,
        IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer,
                IKafkaProducer<string, PersonFolioLinkageModel> folioLinkageProducer,

        DisclosureDataStoreDbContext context) : base(edtClient, configuration, folioLinkageProducer)
    {

        this.configuration = configuration;
        this.context = context;
        this.clock = clock;
        this.mapper = mapper;
        this.logger = logger;
        this.edtClient = edtClient;
        this.producer = producer;
        this.processResponseProducer = processResponseProducer;
        this.folioLinkageProducer = folioLinkageProducer;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, EdtDisclosureDefenceUserProvisioningModel accessRequestModel)
    {

        // check this message is for us


        if (accessRequestModel.SystemName != null && !(accessRequestModel.SystemName.Equals("DigitalEvidenceDisclosure", StringComparison.Ordinal)))
        {
            Serilog.Log.Logger.Information($"Ignoring message {key} for system {accessRequestModel.SystemName} as we only handle DigitalEvidenceDisclosure requests");
            return Task.CompletedTask;
        }


        // set activity info
        Activity.Current?.AddTag("digitalevidence.access.id", accessRequestModel.AccessRequestId);


        using var trx = this.context.Database.BeginTransaction();
        try
        {
            //check whether this message has been processed before   
            if (await this.context.HasBeenProcessed(key, consumerName))
            {
                await trx.RollbackAsync();
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
                //add to tell message has been processed by consumer
                await this.context.IdempotentConsumer(messageId: key, consumer: consumerName, consumeDate: this.clock.GetCurrentInstant());

                await this.context.SaveChangesAsync();
                CaseModel existingFolio = null;

                // we'll track the created case Id so that we can later notify core if necessary to add as LinkDicsloureCaseId to the participant in core
                var processResponseData = new Dictionary<string, string>();

                if (this.configuration.EdtClient.CreateUserFolios)
                {
                    if (!string.IsNullOrEmpty(accessRequestModel.EdtExternalIdentifier))
                    {
                        Serilog.Log.Information($"User {accessRequestModel.ManuallyAddedParticipantId} was manually added - checking for folio with identifier {accessRequestModel.EdtExternalIdentifier}");
                        existingFolio = await this.edtClient.FindCaseByKey(accessRequestModel.EdtExternalIdentifier);
                        if (existingFolio == null)
                        {
                            Serilog.Log.Information($"No folio was found for {accessRequestModel.FullName} - folio will be created");
                        }
                    }

                    if (existingFolio == null)
                    {
                        // get the folio for the user (if present)
                        existingFolio = await this.edtClient.FindCaseByKey(accessRequestModel.Key);
                    }



                    if (existingFolio == null)
                    {
                        Serilog.Log.Information($"User with key {accessRequestModel.Key} does not currently have a folio - adding folio");
                        var folio = await this.CreateUserFolio(accessRequestModel);
                        var linked = await this.LinkUserToFolio(accessRequestModel, folio.Id);
                        processResponseData.Add("caseID", "" + folio.Id);

                    }
                    else
                    {
                        Serilog.Log.Information($"User with key {accessRequestModel.Key} has a folio - adding defence user to folio if not already linked");
                        var linked = await this.LinkUserToFolio(accessRequestModel, existingFolio.Id);
                        processResponseData.Add("caseID", "" + existingFolio.Id);
                    }
                }

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


                    // send a response that the process is complete
                    var msgKey = Guid.NewGuid().ToString();
                    var sentStatus = await this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, msgKey, new GenericProcessStatusResponse
                    {
                        DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidencedisclosure-defence-usercreation-complete" : "digitalevidencedisclosure-defence-usermodification-complete",
                        Id = accessRequestModel.AccessRequestId,
                        EventTime = SystemClock.Instance.GetCurrentInstant(),
                        Status = "Complete-Pending-Folio-Linkage",
                        ResponseData = processResponseData,
                        TraceId = key
                    });

                    if (sentStatus.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                    {
                        Serilog.Log.Information($"{msgKey} successfully published to {this.configuration.KafkaCluster.ProcessResponseTopic} part {sentStatus.Partition.Value}");
                        await trx.CommitAsync();

                    }
                    else
                    {
                        Serilog.Log.Error($"Failed to published {msgKey} to {this.configuration.KafkaCluster.ProcessResponseTopic}");

                    }



                }
                catch (Exception ex)
                {
                    Serilog.Log.Logger.Error($"Failed to publish to user notification topic - rolling back transaction [{string.Join(",", ex.Message)}");
                    await trx.RollbackAsync();
                }
            }
            else
            {
                // send error response
                var sentStatus = this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
                {
                    DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidencedisclosure-defence-usercreation-error" : "digitalevidencedisclosure-defence-usermodification-error",
                    Id = accessRequestModel.AccessRequestId,
                    EventTime = SystemClock.Instance.GetCurrentInstant(),
                    ErrorList = result.Errors,
                    Status = "Error",
                    TraceId = key
                });

            }


        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error("Exception during EDT Disclosure provisioning {0}", ex.Message);
            // send error response
            var sentStatus = this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
            {
                DomainEvent = "digitalevidencedisclosure-defence-usercreation-exception",
                Id = accessRequestModel.AccessRequestId,
                EventTime = SystemClock.Instance.GetCurrentInstant(),
                ErrorList = new List<string>() { ex.Message },
                Status = "Error",
                TraceId = key
            });
            return Task.FromException(ex);
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

