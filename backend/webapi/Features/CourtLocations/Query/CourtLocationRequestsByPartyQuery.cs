namespace Pidp.Features.CourtLocations.Query;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Models;
using Prometheus;

public class CourtLocationRequestsByPartyQuery
{
    public sealed record Query(int PartyId, bool IncludeDeleted) : IQuery<List<CourtLocationAccessModel>>;
    private static readonly Histogram PartyLocationRequestDuration =
        Metrics.CreateHistogram("pidp_court_access_request_list_duration", "Histogram of court location lookups by party.");
    private static readonly Counter PartyLocationRequestCount = Metrics
    .CreateCounter("pidp_court_access_request_count", "Count of court location lookups by party.");

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.PartyId).NotEmpty();
        }
    }

    public class QueryHandler : IQueryHandler<Query, List<CourtLocationAccessModel>>
    {
        private readonly PidpDbContext context;


        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<CourtLocationAccessModel>> HandleAsync(Query query)
        {

            using (PartyLocationRequestDuration.NewTimer())
            {
                PartyLocationRequestCount.Inc();
                return await this.context.CourtLocationAccessRequests
                    .Include(courtLocation => courtLocation.CourtLocation)
                .Where(request => request.PartyId == query.PartyId && (!query.IncludeDeleted ? request.DeletedOn == null : true))
                .Select(caseRequest => new CourtLocationAccessModel
                {
                    CourtLocation = caseRequest.CourtLocation,
                    CourtSubLocation = caseRequest.CourtSubLocation,
                    RequestStatus = caseRequest.RequestStatus,
                    PartyId = caseRequest.PartyId,
                    RequestedOn = caseRequest.RequestedOn,
                    RequestId = caseRequest.RequestId,
                    ValidFrom = caseRequest.ValidFrom,
                    ValidUntil = caseRequest.ValidUntil
                })
                .ToListAsync();
            }
        }
    }

}
