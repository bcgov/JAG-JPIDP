namespace Pidp.Features.CourtLocations.Jobs;

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Pidp.Data;
using Prometheus;
using Quartz;

public class CourtAccessScheduledJob : IJob
{
    private readonly ICourtAccessService courtAccessService;

    public CourtAccessScheduledJob(ICourtAccessService courtAccessService) => this.courtAccessService = courtAccessService;


    private static readonly Histogram CourtLocationScheduledJobDuration = Prometheus.Metrics
         .CreateHistogram("pidp_court_location_scheduled_jobs", "Histogram of court location request job executions");


    public Task Execute(IJobExecutionContext context)
    {

        using (CourtLocationScheduledJobDuration.NewTimer())
        {
            // look for any requests due today that are either provision or remove events
            var pendingRequests = this.courtAccessService.GetRequestsDueToday().Result;

            foreach (var request in pendingRequests)
            {
                Serilog.Log.Information($"Processing request {request.RequestId} From {request.ValidFrom} To {request.ValidUntil}");

                if (request.ValidUntil <= DateTime.UtcNow)
                {
                    Serilog.Log.Information($"Decommission request for {request.RequestId}");
                    this.courtAccessService.CreateRemoveCourtAccessDomainEvent(request);

                }
                else if (request.ValidFrom >= DateTime.UtcNow)
                {
                    Serilog.Log.Information($"Provision request for {request.RequestId}");
                    this.courtAccessService.CreateAddCourtAccessDomainEvent(request);

                }
            }


            return Task.CompletedTask;
        }

    }
}
