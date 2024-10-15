namespace Pidp.Features.SanityChecks.Onboarding;

using System.Threading.Tasks;
using Pidp.Features.SanityChecks.CaseRequests;
using Quartz;

[PersistJobDataAfterExecution]
[DisallowConcurrentExecution]
public class SanityCheckTask(ILogger<SanityCheckTask> logger, ICaseSanityChecks caseSanityChecks) : IJob
{

    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Sanity check task started");

        var updates = caseSanityChecks.HandlePendingCases();


        return Task.CompletedTask;
    }

}
