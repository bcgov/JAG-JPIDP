namespace jumwebapi.Features.Cases.Queries;

using System.Threading;
using System.Threading.Tasks;
using global::Common.Models.JUSTIN;
using jumwebapi.Infrastructure.HttpClients.JustinCases;
using MediatR;
using Prometheus;

public record GetCaseStatusQuery(string caseId) : IRequest<CaseStatus>;


public class GetCaseStatusQueryHandler : IRequestHandler<GetCaseStatusQuery, CaseStatus>
{

    private readonly IJustinCaseClient justinCaseClient;
    private static readonly Counter CaseStatusSearchCount = Metrics
    .CreateCounter("justin_case_status_search_count", "Number of justin case status searches");
    private static readonly Histogram CaseStatusDuration = Metrics
        .CreateHistogram("justin_case_status_search_duration", "Histogram of justin case searches.");
    public GetCaseStatusQueryHandler(IJustinCaseClient justinCaseClient)
    {
        this.justinCaseClient = justinCaseClient;
    }


    public async Task<CaseStatus> Handle(GetCaseStatusQuery request, CancellationToken cancellationToken)
    {
        using (CaseStatusDuration.NewTimer())
        {
            CaseStatusSearchCount.Inc();
            return await this.justinCaseClient.GetCaseStatus(request.caseId, "");
        }
    }
}
