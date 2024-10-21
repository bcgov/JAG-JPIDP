namespace Pidp.Features.DigitalEvidenceCaseManagement.BackgroundServices;

using Common.Kafka;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Models;
using Prometheus;
using Quartz;

[PersistJobDataAfterExecution]
[DisallowConcurrentExecution]
public class SyncCaseAccessJob(PidpDbContext dbContext,
    IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer,
    PidpConfiguration config, ILogger<SyncCaseAccessJob> logger,
    IServiceScopeFactory serviceScopeFactory, IClock clock, IEdtCoreClient
     coreClient, IEdtCaseManagementClient caseManagementClient) : IJob
{
    private const string AUTOSYNC = "AutoSync";
    private const string COMPLETE = "Complete";
    private const string DELETED = "Deleted";
    private const string AUF_TOOLS_CASE = "AUF Tools Case";
    private static readonly Counter UpdatedUsersCaseCount = Metrics.CreateCounter("edt_sync_cases_users_total", "Number of users with case sync updates");
    private static readonly Histogram UpdatedUsersCaseTiming = Metrics.CreateHistogram("edt_sync_cases_users_timing", "Timing of case syncing");


    public async Task Execute(IJobExecutionContext context)
    {

        logger.LogInformation("Sync case access job started");

        // loop through all known agency users
        var partyIds = await dbContext.SubmittingAgencyRequests.Select(p => p.PartyId).Distinct().ToListAsync();
        using (UpdatedUsersCaseTiming.NewTimer())
        {
            foreach (var partyId in partyIds)
            {
                var party = await dbContext.Parties.FirstOrDefaultAsync(p => p.Id == partyId);

                if (party == null)
                {
                    logger.LogWarning($"Party {partyId} not found");
                    continue;
                }

                logger.LogInformation($"Checking case access for party {partyId} {party.Jpdid}");
                // get the edt person

                var edtUser = await coreClient.GetUserByKey(party.Jpdid);

                if (edtUser == null)
                {
                    logger.LogWarning($"User {party.Jpdid} not found in EDT");
                    continue;
                }
                else
                {
                    if (edtUser.IsActive == false)
                    {
                        logger.LogWarning($"User {party.Jpdid} is not active in EDT");
                        continue;
                    }
                    logger.LogInformation($"Getting current state of {edtUser.UserName} {edtUser.FullName} {edtUser.Id}");
                    var cases = await coreClient.GetUserCases(edtUser.Id);

                    var edtCaseIds = cases.Select(c => c.Id).ToList();

                    // get all diam cases - we only care about the ones that are not deleted and not created in the last 30 seconds
                    var knownDIAMCaseIds = dbContext.SubmittingAgencyRequests
                        .Include(sar => sar.Party)
                        .Where(sar => sar.Party.Jpdid == edtUser.UserName && sar.RequestStatus != DELETED)
                        .Where(sar => sar.Created <= clock.GetCurrentInstant().Minus(Duration.FromSeconds(30)))
                        .Select(c => c.CaseId).ToList();

                    var casesInEdtNotInDIAM = edtCaseIds.Except(knownDIAMCaseIds).ToList();
                    var casesInDIAMNotInEdt = knownDIAMCaseIds.Except(edtCaseIds).ToList();

                    // if no count differences then no changes necessary
                    if (casesInEdtNotInDIAM.Count == 0 && casesInDIAMNotInEdt.Count == 0)
                    {
                        logger.LogInformation($"User {edtUser.UserName} has no changes to case access");
                        continue;
                    }

                    foreach (var edtCaseId in casesInEdtNotInDIAM)
                    {
                        logger.LogInformation($"User {edtUser.UserName} has access to case {edtCaseId} that is unknown to DIAM");
                        // get the case info and add as a requested case
                        var caseInfo = await caseManagementClient.GetCase(edtCaseId);
                        if (caseInfo != null)
                        {
                            logger.LogInformation($"AutoSync adding case {edtCaseId} {caseInfo.AgencyFileNumber} for user {edtUser.UserName}");
                            SubmittingAgencyRequest agencyRequest = new SubmittingAgencyRequest()
                            {
                                AgencyFileNumber = (caseInfo.Id == config.AUFToolsCaseId) ? AUF_TOOLS_CASE : caseInfo.AgencyFileNumber,
                                CaseId = edtCaseId,
                                PartyId = partyId,
                                Created = clock.GetCurrentInstant(),
                                RequestedOn = clock.GetCurrentInstant(),
                                RequestStatus = COMPLETE,
                                Details = AUTOSYNC
                            };
                            var added = dbContext.SubmittingAgencyRequests.AddAsync(agencyRequest);

                        }
                    }

                    foreach (var diamCase in casesInDIAMNotInEdt)
                    {
                        logger.LogInformation($"User {edtUser.UserName} has DIAM case {diamCase} but does not have access to case in EDT - flagging deleted");
                        // remove the case from DIAM
                        var request = await dbContext.SubmittingAgencyRequests.Include(c => c.Party).Where(c => c.RequestStatus != DELETED && c.DeletedOn == null).FirstOrDefaultAsync(sar => sar.CaseId == diamCase && sar.Party.Jpdid == edtUser.UserName);
                        if (request != null)
                        {
                            request.DeletedOn = clock.GetCurrentInstant();
                            request.RequestStatus = DELETED;
                            request.Details = AUTOSYNC;
                        }
                    }

                    var updatedRows = await dbContext.SaveChangesAsync();
                    logger.LogInformation($"Affected {updatedRows} row(s) for user {edtUser.UserName} during auto-sync");
                    UpdatedUsersCaseCount.Inc(updatedRows);
                }


            }
        }
    }
}
