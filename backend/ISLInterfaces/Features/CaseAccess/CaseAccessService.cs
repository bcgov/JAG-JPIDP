namespace ISLInterfaces.Features.CaseAccess;

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using ISLInterfaces.Data;
using ISLInterfaces.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 
/// </summary>
public class CaseAccessService(DiamReadOnlyContext context, ILogger<CaseAccessService> logger, Instrumentation instrumentation) : ICaseAccessService
{

    private readonly DiamReadOnlyContext context = context;
    private readonly ILogger<CaseAccessService> logger = logger;
    private readonly Counter<long> caseSearchCount = instrumentation.CaseSearchCount;
    private readonly Counter<long> caseActiveUsersCount = instrumentation.CaseActiveUsersCount;


    /// <summary>
    /// Get users that are currently active on a case
    /// </summary>
    /// <param name="rccNumber"></param>
    /// <returns></returns>
    public async Task<List<string>> GetCaseAccessUsersAsync(string rccNumber)
    {
        this.logger.LogInformation($"Getting users on case {rccNumber}");
        this.caseSearchCount.Add(1);

        if (!this.context.Database.CanConnect())
        {
            logger.LogError("Unable to connect to database - check connection info");
        }

        List<string?> results = [];

        results = await this.context.SubmittingAgencyRequests
         .Where(access => access.RCCNumber == rccNumber && (access.RequestStatus == AgencyRequestStatus.Complete || access.RequestStatus == AgencyRequestStatus.Pending || access.RequestStatus == AgencyRequestStatus.Submitted))
         .Include(party => party.Party)
         .OrderBy(access => access.RequestedOn)
                     .Select(access => access.Party.Jpdid)
                     .ToListAsync();
        this.caseActiveUsersCount.Add(results.Count);
        return results;

    }
}
