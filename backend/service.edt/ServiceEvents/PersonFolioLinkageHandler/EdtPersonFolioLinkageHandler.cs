namespace edt.service.ServiceEvents.PersonFolioLinkageHandler;

using Common.Models.EDT;
using edt.service.Data;
using edt.service.Exceptions;
using edt.service.Kafka.Interfaces;
using edt.service.ServiceEvents.PersonCreationHandler.Models;
using edt.service.ServiceEvents.UserAccountCreation.Models;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;

/// <summary>
/// This class will recieve an incoming request to link a person(participant) to a given disclosure folio
/// It is then the responsibility of the FolioLinkageBackgroundService to do that actual linkage in Core
/// </summary>
public class EdtPersonFolioLinkageHandler : IKafkaHandler<string, PersonFolioLinkageModel>
{

    private readonly ILogger logger;
    private readonly IClock clock;
    private readonly EdtDataStoreDbContext context;
    private static readonly Histogram ProcessFolioRequestDuration = Metrics.CreateHistogram("edt_folio_incoming_linkage_request", "Histogram of edt folio linkage requests.");

    public EdtPersonFolioLinkageHandler(
        EdtDataStoreDbContext context,
        IClock clock,
        ILogger logger)
    {
        this.context = context;
        this.clock = clock;
        this.logger = logger;
    }
    public async Task<Task> HandleAsync(string consumerName, string key, PersonFolioLinkageModel accessRequestModel)
    {
        this.logger.LogMessageReceived(key);

        // check message not received before
        using var trx = this.context.Database.BeginTransaction();
        using (ProcessFolioRequestDuration.NewTimer())
        {
            try
            {
                //check whether this message has been processed before   
                if (await this.context.HasBeenProcessed(key, consumerName))
                {
                    await trx.RollbackAsync();
                    return Task.CompletedTask;
                }

                // store record in folio linkage table - will be picked up by scheduler to handle
                this.context.Add(new PersonFolioLinkage
                {
                    Created = this.clock.GetCurrentInstant(),
                    PersonKey = accessRequestModel.PersonKey,
                    PersonType = accessRequestModel.PersonType,
                    Status = accessRequestModel.Status,
                    DisclosureCaseIdentifier = accessRequestModel.DisclosureCaseIdentifier,
                    EdtExternalId = accessRequestModel.EdtExternalId
                });

                // store idempotent record
                this.context.IdempotentConsumers.Add(new IdempotentConsumer
                {
                    Consumer = consumerName,
                    MessageId = key
                });

                await this.context.SaveChangesAsync();
                await trx.CommitAsync();

                var pendingRequests = this.context.FolioLinkageRequests.Where(req => req.Status == "Pending").Count();
                this.logger.LogPendingRequestCount(pendingRequests);


                return Task.CompletedTask;

            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                throw new EdtServiceException($"Exception during FolioLinkage handler processing {ex.Message}");

            }
        }
    }
    public Task<Task> HandleRetryAsync(string consumerName, string key, PersonFolioLinkageModel value, int retryCount, string topicName) => throw new NotImplementedException("Currently not implemented as this is a bad retry design");

}


public static partial class FolioLinkageHandlerLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Folio linkage message recieved {key}")]
    public static partial void LogMessageReceived(this ILogger logger, string key);
    [LoggerMessage(2, LogLevel.Information, "{count} pending folio linkage requests")]
    public static partial void LogPendingRequestCount(this ILogger logger, int count);
}
