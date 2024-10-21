namespace jumwebapi.Features.Cases.Queries;

using System.Threading;
using System.Threading.Tasks;
using CommonModels.Models.JUSTIN;
using jumwebapi.Infrastructure.HttpClients.JustinCases;
using MediatR;
using Prometheus;

public record GetCaseStatusQuery(string caseId) : IRequest<CaseStatusWrapper>;


public class GetCaseStatusQueryHandler : IRequestHandler<GetCaseStatusQuery, CaseStatusWrapper>
{

    private readonly IJustinCaseClient justinCaseClient;
    private static readonly Counter CaseStatusSearchCount = Metrics
    .CreateCounter("justin_case_status_search_total", "Number of justin case status searches");
    private static readonly Histogram CaseStatusDuration = Metrics
        .CreateHistogram("justin_case_status_search_duration", "Histogram of justin case searches.");
    public GetCaseStatusQueryHandler(IJustinCaseClient justinCaseClient)
    {
        this.justinCaseClient = justinCaseClient;
    }


    public async Task<CaseStatusWrapper> Handle(GetCaseStatusQuery request, CancellationToken cancellationToken)
    {
        using (CaseStatusDuration.NewTimer())
        {
            CaseStatusSearchCount.Inc();
            return await this.justinCaseClient.GetCaseStatus(request.caseId, "");
        }
    }
}
