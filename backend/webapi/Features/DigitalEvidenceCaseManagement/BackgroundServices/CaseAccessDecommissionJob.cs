namespace Pidp.Features.DigitalEvidenceCaseManagement.BackgroundServices;

using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Quartz;

[PersistJobDataAfterExecution]
[DisallowConcurrentExecution]
public class CaseAccessDecommissionJob(PidpDbContext dbContext, IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer, PidpConfiguration config, ILogger<CaseAccessDecommissionJob> logger, IServiceScopeFactory serviceScopeFactory, IClock clock) : IJob
{

    /// <summary>
    /// Publish the request to the agency topic and ensure delivery is successful
    /// </summary>
    /// <param name="subAgencyRequests"></param>
    /// <returns></returns>
    private async Task<DeliveryResult<string, SubAgencyDomainEvent>> PublishSubAgencyAccessRequest(SubmittingAgencyRequest caseAccessRequest)
    {

        var publishResponse = await kafkaProducer.ProduceAsync(config.KafkaCluster.CaseAccessRequestTopicName, $"{caseAccessRequest.RequestId}", new SubAgencyDomainEvent
        {
            RequestId = caseAccessRequest.RequestId,
            PartyId = caseAccessRequest.PartyId,
            EventType = CaseEventType.Decommission,
            CaseId = caseAccessRequest.CaseId,
            Username = caseAccessRequest.Party!.Jpdid,
            UserId = caseAccessRequest.Party!.UserId,
            RequestedOn = caseAccessRequest.RequestedOn,
        });


        return publishResponse;

    }

    /// <summary>
    /// Find any expired case requests
    /// </summary>
    /// <returns></returns>
    private async Task<IEnumerable<SubmittingAgencyRequest>> GetExpiredCaseAccessRequests()
    {
        return await dbContext.SubmittingAgencyRequests
            .Where(request => request.DeletedOn == null && request.RequestedOn < clock.GetCurrentInstant().Minus(Duration.FromDays(config.BackGroundServices.DecomissionCaseAccessService.GracePeriod)))
            .Include(party => party.Party)
            .ToListAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogExecutingJob();
        var expiredAccessRequests = await this.GetExpiredCaseAccessRequests();

        if (expiredAccessRequests.Any())
        {
            logger.LogFoundExpiredCases(expiredAccessRequests.Count());

            // go through all expired requests and remove them
            foreach (var request in expiredAccessRequests)
            {
                logger.LogProcessingCase(request.RequestId, request.AgencyFileNumber, request.Party!.Jpdid!);

                var published = await this.PublishSubAgencyAccessRequest(request);

                // we'll only remove the request if it was successfully published
                if (published.Status == PersistenceStatus.Persisted)
                {
                    // flag case as pending deletion
                    request.RequestStatus = AgencyRequestStatus.RemovalPending;
                    dbContext.SubmittingAgencyRequests.Update(request);
                }
                else
                {
                    logger.LogFailedToPublish(request.RequestId, request.AgencyFileNumber, request.Party!.Jpdid!);
                }
            }

            // check how many were successfully removed
            var removedRows = await dbContext.SaveChangesAsync();

            if (removedRows == expiredAccessRequests.Count())
            {
                logger.LogComplete(removedRows, expiredAccessRequests.Count());

            }
            else
            {
                logger.LogNotAllSuccessful(removedRows, expiredAccessRequests.Count());

            }
        }
    }
}
public static partial class CaseAccessDecommissionJobLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Executing Case Decommission Job")]
    public static partial void LogExecutingJob(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Found {foundCases} expired case access requests")]
    public static partial void LogFoundExpiredCases(this ILogger logger, int foundCases);

    [LoggerMessage(3, LogLevel.Information, "Processing remove request {requestId} for case {agencyNumber} - Party {partyName}")]
    public static partial void LogProcessingCase(this ILogger logger, int requestId, string agencyNumber, string partyName);

    [LoggerMessage(4, LogLevel.Error, "Failed to publish remove request {requestId} for case {agencyNumber} - Party {partyName}")]
    public static partial void LogFailedToPublish(this ILogger logger, int requestId, string agencyNumber, string partyName);

    [LoggerMessage(5, LogLevel.Information, "Successfully processed {removedRows} expired cases out of {foundCases}")]
    public static partial void LogComplete(this ILogger logger, int removedRows, int foundCases);


    [LoggerMessage(6, LogLevel.Warning, "Successfully processed {removedRows} expired cases out of {foundCases}")]
    public static partial void LogNotAllSuccessful(this ILogger logger, int removedRows, int foundCases);
}
