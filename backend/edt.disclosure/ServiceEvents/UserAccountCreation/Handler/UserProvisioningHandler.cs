namespace edt.disclosure.ServiceEvents.UserAccountCreation.Handler;

using System.Diagnostics;
using AutoMapper;
using edt.disclosure.Data;
using edt.disclosure.Exceptions;
using edt.disclosure.Features.Cases;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Kafka.Model;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.UserAccountCreation.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;
using NodaTime;

public class UserProvisioningHandler : IKafkaHandler<string, EdtDisclosureUserProvisioningModel>
{


    private readonly EdtDisclosureServiceConfiguration configuration;
    private readonly IEdtDisclosureClient edtClient;
    private readonly IMapper mapper;

    private readonly IClock clock;
    private readonly ILogger logger;
    private readonly DisclosureDataStoreDbContext context;
    private readonly IKafkaProducer<string, Notification> producer;
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer;


    public UserProvisioningHandler(
        EdtDisclosureServiceConfiguration configuration,
        IEdtDisclosureClient edtClient,
        IClock clock,
        IMapper mapper,
        ILogger logger,
        IKafkaProducer<string, Notification> producer,
        IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer,
        DisclosureDataStoreDbContext context)
    {

        this.configuration = configuration;
        this.context = context;
        this.clock = clock;
        this.mapper = mapper;
        this.logger = logger;
        this.edtClient = edtClient;
        this.producer = producer;
        this.processResponseProducer = processResponseProducer;

    }

    public async Task<Task> HandleAsync(string consumerName, string key, EdtDisclosureUserProvisioningModel accessRequestModel)
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
                await this.context.IdempotentConsumer(messageId: key, consumer: consumerName, consumeDate: clock.GetCurrentInstant());

                await this.context.SaveChangesAsync();

                // get the folio for the user (if present)
                var existingFolio = await this.edtClient.FindCaseByKey(accessRequestModel.Key);

                // we'll track the created case Id so that we can later notify core if necessary to add as LinkDicsloureCaseId to the participant in core
                var processResponseData = new Dictionary<string, string>();

                if (existingFolio == null)
                {
                    Serilog.Log.Information($"User with key {accessRequestModel.Key} does not currently have a folio - adding folio");
                    var folio = await this.CreateUserFolio(accessRequestModel);
                    var linked = await this.LinkUserToFolio(accessRequestModel, folio.Id);
                    processResponseData.Add("caseID", ""+ folio.Id);

                }
                else
                {
                    Serilog.Log.Information($"User with key {accessRequestModel.Key} has a folio - adding user to folio if not already linked");
                    var linked = await this.LinkUserToFolio(accessRequestModel, existingFolio.Id);
                    processResponseData.Add("caseID", "" + existingFolio.Id);
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
                    var sentStatus = this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
                    {
                        DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidencedisclosure-defence-usercreation-complete" : "digitalevidencedisclosure-defence-usermodification-complete",
                        Id = accessRequestModel.AccessRequestId,
                        EventTime = SystemClock.Instance.GetCurrentInstant(),
                        Status = "Complete",
                        ResponseData = processResponseData,
                        TraceId = key
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
            return Task.FromException(ex);
        }

        return Task.CompletedTask; //create specific exception handler later
    }


    private async Task<CaseModel> CreateUserFolio(EdtDisclosureUserProvisioningModel accessRequestModel)
    {
        // check case isnt present
        var caseModel = await this.edtClient.FindCaseByKey(accessRequestModel.Key);
        if (caseModel != null)
        {
            return caseModel;
        }

        var caseName = accessRequestModel.FullName + " (Defence Folio)";
        var caseCreation = new EdtCaseDto
        {
            Name = caseName,
            Description = "Folio Case for Defence Counsel",
            Key = accessRequestModel.Key,
            TemplateCase = (this.configuration.EdtClient.DefenceFolioTemplateId < 0 && !string.IsNullOrEmpty(this.configuration.EdtClient.DefenceFolioTemplateName)) ? this.configuration.EdtClient.DefenceFolioTemplateName : null,
            TemplateCaseId = (this.configuration.EdtClient.DefenceFolioTemplateId > -1 ) ? this.configuration.EdtClient.DefenceFolioTemplateId.ToString() : null,

        };

        Serilog.Log.Information($"Creating new case {caseCreation}");

        var createResponseID = await this.edtClient.CreateCase(caseCreation);

        caseModel = await this.edtClient.GetCase(createResponseID);


        return caseModel;


    }

    public async Task<bool> LinkUserToFolio(EdtDisclosureUserProvisioningModel accessRequestModel, int caseId)
    {
        var addedOk = false;

        var user = await this.edtClient.GetUser(accessRequestModel.Key!) ?? throw new EdtDisclosureServiceException($"User was not found {accessRequestModel.Key}");


        if (this.configuration.EdtClient.DefenceCaseGroup != null)
        {
            addedOk = await this.edtClient.AddUserToCase(user.Id, caseId, this.configuration.EdtClient.DefenceCaseGroup);
        }
        else
        {
            Serilog.Log.Warning($"*** NO case group for defence assigned in configuration - no case group will be assigned for user {user.Id} and case {caseId} ***");
            addedOk = await this.edtClient.AddUserToCase(user.Id, caseId);
        }

        if (!addedOk)
        {
            throw new EdtDisclosureServiceException($"Failed to add user {user.Id} to defence folio");
        }
        return true;
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

