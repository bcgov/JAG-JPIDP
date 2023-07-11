namespace Pidp.Features.CourtLocations.Jobs;

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Pidp.Data;
using Pidp.Models;
using Prometheus;
using Quartz;

public class CourtAccessScheduledJob : IJob
{
    private readonly ICourtAccessService courtAccessService;

    public CourtAccessScheduledJob(ICourtAccessService courtAccessService) => this.courtAccessService = courtAccessService;


    private static readonly Histogram CourtLocationScheduledJobDuration = Prometheus.Metrics
         .CreateHistogram("pidp_court_location_scheduled_jobs", "Histogram of court location request job executions");


    public async Task Execute(IJobExecutionContext context)
    {

        using (CourtLocationScheduledJobDuration.NewTimer())
        {
            // look for any requests due today that are either provision or remove events
            var pendingRequests = this.courtAccessService.GetRequestsDueToday().Result;

            foreach (var request in pendingRequests)
            {
                Serilog.Log.Information($"Processing request {request.RequestId} From {request.ValidFrom} To {request.ValidUntil}");

                if (request.ValidUntil <= DateTime.UtcNow && request.RequestStatus != CourtLocationAccessStatus.RemovalPending)
                {
                    Serilog.Log.Information($"Decommission request for {request.RequestId}");
                    var taskStatus = await this.courtAccessService.CreateRemoveCourtAccessDomainEvent(request);
                    if (taskStatus)
                    {
                        var updated = await this.courtAccessService.SetAccessRequestStatus(request, CourtLocationAccessStatus.RemovalPending.ToString());
                        if (!updated)
                        {
                            Serilog.Log.Error($"Failed to set status of {request.RequestId} to {CourtLocationAccessStatus.RemovalPending}");
                        }

                    }
                }
                else if (request.ValidFrom <= DateTime.UtcNow && request.ValidUntil >= DateTime.Now
                    && request.RequestStatus != CourtLocationAccessStatus.RemovalPending
                    && request.RequestStatus != CourtLocationAccessStatus.Submitted
                    && request.RequestStatus != CourtLocationAccessStatus.Deleted)
                {
                    Serilog.Log.Information($"Provision request for {request.RequestId}");
                    await this.courtAccessService.CreateAddCourtAccessDomainEvent(request);
                }
                else
                {
                    Serilog.Log.Information($"Skipping request for {request.RequestId} with status {request.RequestStatus}");
                }
            }

        }

    }
}
