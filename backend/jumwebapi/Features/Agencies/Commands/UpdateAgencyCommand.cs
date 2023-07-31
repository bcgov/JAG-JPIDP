using jumwebapi.Data.ef;
using jumwebapi.Features.Agencies.Services;
using MediatR;

namespace jumwebapi.Features.Agencies.Commands;

public sealed record UpdateAgencyCommand(long AgencyId, string Name, string AgencyCode, string Description) : IRequest<JustinAgency>;
public class UpdateAgencyCommandHandler : IRequestHandler<UpdateAgencyCommand, JustinAgency>
{
    private IAgencyService _agencyService;
    public UpdateAgencyCommandHandler(IAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    public async Task<JustinAgency> Handle(UpdateAgencyCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencyService.AgencyById(request.AgencyId);
        if (agency == null) return default ;
        agency.Name = request.Name;
        agency.Description = request.Description;
        agency.AgencyCode = request.AgencyCode;
        return await _agencyService.UpdateAgency(agency);

    }
}
