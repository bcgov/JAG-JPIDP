namespace Pidp.Features.SanityChecks.CaseRequests;

using Common.Kafka;
using Common.Models.Notification;
using CommonConstants.Constants.Status;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Models;

public class CaseSanityChecks(ILogger<CaseSanityChecks> logger, PidpDbContext dbContext, IClock clock, IKafkaProducer<string, SubAgencyDomainEvent> producer, PidpConfiguration config, IKafkaProducer<string, Notification> notificationProducder) : ICaseSanityChecks
{
    public async Task<int> HandlePendingCases()
    {
        // get current time less five minutes
        var republishTimeout = clock.GetCurrentInstant().Minus(Duration.FromMinutes(config.SanityCheck.RepublishDelayMinutes));

        var flagErrorTimeout = clock.GetCurrentInstant().Minus(Duration.FromMinutes(config.SanityCheck.FailureDelayMinutes));

        var longRunningCases = dbContext.SubmittingAgencyRequests.Include(r => r.Party).Where(c => c.RequestStatus != EventStatus.Complete && c.RequestStatus != EventStatus.Deleted && c.Modified <= flagErrorTimeout).ToList();
        if (longRunningCases.Count > 0)
        {
            logger.LogError($"*** We have {longRunningCases} that have not completed within 15 minutes ***");


        }

        // find any cases that are not complete or deleted
        var cases = dbContext.SubmittingAgencyRequests.Include(r => r.Party).Where(c => c.RequestStatus != EventStatus.Complete && c.RequestStatus != EventStatus.Deleted && c.Modified <= republishTimeout).ToList();
        var fixesCases = 0;
        if (cases.Count == 0)
        {
            logger.LogDebug($"No cases found that are not complete as of {republishTimeout.ToJulianDate()}");
            return 0;
        }
        else
        {
            logger.LogInformation($"We have {cases.Count} that are not complete as of {republishTimeout.ToJulianDate()}");

            // loop through all cases that are not complete
            foreach (var subAgencyRequest in cases)
            {
                logger.LogInformation($"Processing case {subAgencyRequest.RequestId} for {subAgencyRequest.PartyId} with status of {subAgencyRequest.RequestStatus}");

                var party = await dbContext.Parties.FirstOrDefaultAsync(p => p.Id == subAgencyRequest.PartyId);

                if (party == null)
                {
                    logger.LogWarning($"Party {subAgencyRequest.PartyId} not found for case {subAgencyRequest.RequestId} - removing request");
                    dbContext.SubmittingAgencyRequests.Remove(subAgencyRequest);
                    continue;
                }

                switch (subAgencyRequest.RequestStatus)
                {
                    case EventStatus.Pending:
                    case EventStatus.InProgress:
                        logger.LogWarning($"Case {subAgencyRequest.RequestId} is still pending after 5 minutes - republishing");
                        if (await this.PublishSubAgencyAccessRequest(subAgencyRequest, false))
                        {
                            fixesCases++;
                        }

                        break;
                    case EventStatus.RemovalPending:
                        logger.LogWarning($"Case {subAgencyRequest.RequestId} is removal pending after 5 minutes - republishing");

                        if (await this.PublishSubAgencyAccessRequest(subAgencyRequest, true))
                        {
                            fixesCases++;
                        }


                        break;

                    default:
                        logger.LogWarning($"Case {subAgencyRequest.RequestId} is in state {subAgencyRequest.RequestStatus} that is not handled by sanity check");
                        break;


                }
            }

            if (cases.Count == fixesCases)
            {
                logger.LogInformation($"All {fixesCases} cases were fixed during sanity checks");
            }

            return fixesCases;

        }
    }

    private async Task<bool> PublishSubAgencyAccessRequest(SubmittingAgencyRequest request, bool decommission)
    {
        var msgKey = Guid.NewGuid().ToString();
        Serilog.Log.Logger.Information("Publishing Sub Agency Domain Event to topic {0} {1} {2}", config.KafkaCluster.CaseAccessRequestTopicName, msgKey, request.RequestId);
        var publishEntry = new SubAgencyDomainEvent
        {
            RequestId = request.RequestId,
            CaseId = request.CaseId,
            PartyId = request.PartyId,
            EventType = decommission ? CaseEventType.Decommission : CaseEventType.Provisioning,
            AgencyFileNumber = request.AgencyFileNumber,
            Username = request.Party!.Jpdid,
            UserId = request.Party!.UserId,
            RequestedOn = request.RequestedOn
        };
        var publishResponse = await producer.ProduceAsync(config.KafkaCluster.CaseAccessRequestTopicName, msgKey, publishEntry);

        return publishResponse.Status.Equals(PersistenceStatus.Persisted);
    }
}
