namespace edt.service.ServiceEvents.UserAccountCreation.Handler;

using System.Diagnostics;
using Common.Exceptions.EDT;
using Common.Models.EDT;
using edt.service.Data;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka.Interfaces;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.UserAccountModification.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

public class DefenceParticipantHandler : IKafkaHandler<string, EdtPersonProvisioningModel>
{
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer;
    private readonly IKafkaProducer<string, UserModificationEvent> userModificationProducer;

    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly IClock clock;
    private readonly EdtDataStoreDbContext context;


    public DefenceParticipantHandler(
        IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer,
        IKafkaProducer<string, UserModificationEvent> userModificationProducer,
        EdtServiceConfiguration configuration,
        IEdtClient edtClient,
        IClock clock,
        EdtDataStoreDbContext context, ILogger logger)
    {
        this.configuration = configuration;
        this.context = context;
        this.logger = logger;
        this.clock = clock;
        this.userModificationProducer = userModificationProducer;
        this.edtClient = edtClient;
        this.processResponseProducer = processResponseProducer;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, EdtPersonProvisioningModel accessRequestModel)
    {

        // check this message is for us
        if (accessRequestModel.SystemName != null && !accessRequestModel.SystemName.Equals("DigitalEvidenceDefence", StringComparison.Ordinal))
        {
            Serilog.Log.Logger.Information($"Ignoring message {key} for system {accessRequestModel.SystemName} as we only handle Defence Participant requests");
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
                //await trx.RollbackAsync();
                return Task.CompletedTask;
            }
            ///check whether edt service api is available before making any http request
            ///
            /// call version endpoint via get
            ///
            var edtVersion = await this.CheckEdtServiceVersion();

            // EDT Disclosure Participants should have role = Defence Counsel
            accessRequestModel.Fields.Add(new EdtField
            {
                Name = "Role",
                Value = "Defence Counsel"
            });

            //check whether edt user already exist
            var result = await this.AddOrUpdatePerson(accessRequestModel);


            if (result.successful)
            {
                // send process response to webapi
                var sentStatus = await this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
                {
                    DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidence-defence-personcreation-complete" : "digitalevidence-defence-personmodification-complete",
                    Id = accessRequestModel.AccessRequestId,
                    EventTime = this.clock.GetCurrentInstant(),
                    Status = "Complete",
                    TraceId = key
                });
                if (sentStatus.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                {
                    Serilog.Log.Information($"Success response sent for person creation {accessRequestModel.AccessRequestId} {accessRequestModel.Key}");
                }
                else
                {
                    Serilog.Log.Error($"Failed to send success response for person creation {accessRequestModel.AccessRequestId} {accessRequestModel.Key}");
                }
            }
            else
            {
                // send error process response to webapi
                // send process response to webapi
                var sentStatus = await this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, Guid.NewGuid().ToString(), new GenericProcessStatusResponse
                {
                    DomainEvent = (result.eventType == UserModificationEvent.UserEvent.Create) ? "digitalevidence-defence-personcreation-error" : "digitalevidence-defence-personmodification-error",
                    Id = accessRequestModel.AccessRequestId,
                    EventTime = this.clock.GetCurrentInstant(),
                    Status = "Error",
                    TraceId = key
                });

                if (sentStatus.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                {
                    Serilog.Log.Information($"Error response sent for person creation {accessRequestModel.AccessRequestId} {accessRequestModel.Key}");
                }
                else
                {
                    Serilog.Log.Error($"Failed to send error response for person creation {accessRequestModel.AccessRequestId} {accessRequestModel.Key}");
                }
            }

            //add to tell message has been proccessed by consumer - if errored then the error would need to be handled - likely something on the EDT side
            await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
            await this.context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger.Error("Exception during EDT defence participant provisioning {0}", ex.Message);
            //on exception rollback trx, publish to retry topic for retry and commit offset 
            await trx.RollbackAsync();

            // this.logger.LogUserAccessPublishError(accessRequestModel.Key, key, this.configuration.KafkaCluster.ProducerTopicName, this.configuration.KafkaCluster.InitialRetryTopicName);
        }

        return Task.CompletedTask; //create specific exception handler later
    }

    // need to rethink this retry - outbox model is better
    public Task<Task> HandleRetryAsync(string consumerName, string key, EdtPersonProvisioningModel value, int retryCount, string topicName) => throw new NotImplementedException();

    private async Task<UserModificationEvent> AddOrUpdatePerson(EdtPersonProvisioningModel accessRequestModel)
    {
        Serilog.Log.Information($"Handling request to add/update participant {accessRequestModel.FirstName} {accessRequestModel.LastName} {accessRequestModel.Id}");

        if (accessRequestModel.ManuallyAddedParticipantId > 0)
        {
            Serilog.Log.Information($"Participant {accessRequestModel.LastName} {accessRequestModel.ManuallyAddedParticipantId} was added manually - updating");
            var user = await this.edtClient.GetPersonById(accessRequestModel.ManuallyAddedParticipantId);
            if (user != null)
            {
                return await this.edtClient.ModifyPerson(accessRequestModel, user);
            }
            else
            {
                var errorMsg = $"Manually added {accessRequestModel.ManuallyAddedParticipantId} id was not found";
                Serilog.Log.Error(errorMsg);
                throw new EdtServiceException(errorMsg);
            }
        }
        else
        {

            var user = await this.edtClient.GetPerson(accessRequestModel.Key!);

            if (user == null)
            {
                Serilog.Log.Information($"Adding {accessRequestModel.LastName} as a new person");
                return await this.edtClient.CreatePerson(accessRequestModel);
            }
            else
            {
                Serilog.Log.Information($"Modifying person {accessRequestModel.LastName}");
                return await this.edtClient.ModifyPerson(accessRequestModel, user);
            }
        }

    }

    private async Task<string> CheckEdtServiceVersion() => await this.edtClient.GetVersion();

}
public static partial class DefenceParticipantHandlerLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Cannot provision user with partId {partId} and request Id {accessrequestId}. Published event key {accessrequestId} of {fromTopic} record to {topic} topic for retrial")]
    public static partial void LogUserAccessPublishError(this ILogger logger, string? partId, string accessrequestId, string fromTopic, string topic);

}

