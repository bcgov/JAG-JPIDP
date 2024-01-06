namespace edt.disclosure.ServiceEvents.DefenceUserAccountCreation.Handler;

using System.Diagnostics;
using AutoMapper;
using Common.Models.EDT;
using edt.disclosure.Data;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Kafka.Model;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.UserAccountCreation.Handler;
using edt.disclosure.ServiceEvents.UserAccountCreation.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

public class PublicUserProvisioningHandler : BaseProvisioningHandler, IKafkaHandler<string, EdtDisclosurePublicUserProvisioningModel>
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

    public PublicUserProvisioningHandler(
        EdtDisclosureServiceConfiguration configuration,
        IEdtDisclosureClient edtClient,
        IClock clock,
        IMapper mapper,
        ILogger logger,
        IKafkaProducer<string, PersonFolioLinkageModel> folioLinkageProducer,
        IKafkaProducer<string, Notification> producer,
        IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer,
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
    }

    public async Task<Task> HandleAsync(string consumerName, string key, EdtDisclosurePublicUserProvisioningModel accessRequestModel)
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


            var edtVersion = await this.CheckEdtServiceVersion();

            // add or update user as necessary
            var result = await this.AddOrUpdateUser(accessRequestModel);

            if (result.successful)
            {
                //add to tell message has been processed by consumer
                await this.context.IdempotentConsumer(messageId: key, consumer: consumerName, consumeDate: this.clock.GetCurrentInstant());

                await this.context.SaveChangesAsync();

                // get the folio for the user (if present)
                var existingFolio = await this.edtClient.FindCaseByKey(accessRequestModel.PersonKey);
                // we'll track the created case Id so that we can later notify core if necessary to add as LinkDicsloureCaseId to the participant in core
                var processResponseData = new Dictionary<string, string>
                {
                    { "OOCUniqueId", accessRequestModel.PersonKey }
                };


                if (existingFolio == null)
                {
                    Serilog.Log.Information($"User with key {accessRequestModel.Key} does not currently have a folio - adding folio");
                    var folio = await this.CreateUserFolio(accessRequestModel);
                    var linked = await this.LinkUserToFolio(accessRequestModel, folio.Id);
                    processResponseData.Add("caseID", "" + folio.Id);

                }
                else
                {
                    Serilog.Log.Information($"User with key {accessRequestModel.Key} has a folio - adding public user to folio if not already linked");
                    var linked = await this.LinkUserToFolio(accessRequestModel, existingFolio.Id);
                    processResponseData.Add("caseID", "" + existingFolio.Id);
                }


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
                    DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidencedisclosure-bcsc-usercreation-complete" : "digitalevidencedisclosure-bcsc-usermodification-complete",
                    Id = accessRequestModel.AccessRequestId,
                    EventTime = SystemClock.Instance.GetCurrentInstant(),
                    Status = "Complete",
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
            else

            {
                // send error response
                var sentStatus = this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
                {
                    DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidencedisclosure-bcsc-usercreation-error" : "digitalevidencedisclosure-bcsc-usermodification-error",
                    Id = accessRequestModel.AccessRequestId,
                    EventTime = SystemClock.Instance.GetCurrentInstant(),
                    PartId = accessRequestModel.Id,
                    ErrorList = result.Errors,
                    Status = "Error",
                    TraceId = key
                });
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error("Exception during EDT Public User Disclosure provisioning {0}", ex.Message);
            await trx.RollbackAsync();
            // send error response
            var sentStatus = this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
            {
                DomainEvent = "digitalevidencedisclosure-bcsc-exception",
                Id = accessRequestModel.AccessRequestId,
                PartId = accessRequestModel.Id,
                EventTime = SystemClock.Instance.GetCurrentInstant(),
                ErrorList = new List<string> { ex.Message },
                Status = "Error",
                TraceId = key
            });
            return Task.FromException(ex);
        }


        return Task.CompletedTask;


    }
}
