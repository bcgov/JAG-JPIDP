using jumwebapi.Data.ef;
using jumwebapi.Features.Agencies.Services;
using MediatR;

namespace jumwebapi.Features.Agencies.Queries;

public sealed record AgencyQuery : IRequest<IEnumerable<JustinAgency>>;

public class AgencyQueryHandler : IRequestHandler<AgencyQuery, IEnumerable<JustinAgency>>
{
    private readonly IAgencyService _agencyService;
    public AgencyQueryHandler(IAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    public async Task<IEnumerable<JustinAgency>> Handle(AgencyQuery request, CancellationToken cancellationToken)
    {
        return await _agencyService.GetAllAgencies();
    }
}