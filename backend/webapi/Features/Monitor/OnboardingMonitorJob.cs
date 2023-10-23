namespace Pidp.Features.Monitor;

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Prometheus;
using Quartz;

public class OnboardingMonitorJob : IJob
{
    private readonly PidpDbContext dbContext;

    private static readonly Gauge AwaitingJUSTINCases = Metrics
        .CreateGauge("diam_accessrequest_awaiting_justin", "Number of jobs waiting for JUSTIN completion more than 1 hour old.");

    private static readonly Gauge AwaitingEDTUserCreation = Metrics
        .CreateGauge("diam_accessrequest_awaiting_edt_user_creation", "Number of jobs waiting for EDT completion more than 10 minutes old.");

    public OnboardingMonitorJob(PidpDbContext dbContext) => this.dbContext = dbContext;

    public Task Execute(IJobExecutionContext context)
    {
        Serilog.Log.Debug($"Looking for onboarding issues...");

        this.dbContext.AccessRequests.Include(req => req.Party).Where(req => req.Status != "Complete").ForEachAsync(ar =>
        {
            var duration = new TimeSpan(DateTime.Now.Ticks - ar.Modified.ToDateTimeOffset().Ticks);

            if (ar.Status == "Complete-Pending-Case-Allocation" && duration.TotalMinutes > 120)
            {
                Serilog.Log.Information($"Request {ar.Id} has been in status {ar.Status} for {duration.TotalMinutes} minutes");
                AwaitingJUSTINCases.Inc();
            }
            else if (duration.TotalMinutes > 5)
            {
                AwaitingEDTUserCreation.Inc();
            }


        });

        return Task.CompletedTask;
    }
}
