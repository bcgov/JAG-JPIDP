using jumwebapi.Data.ef;
using jumwebapi.Features.Agencies.Services;
using MediatR;

namespace jumwebapi.Features.Agencies.Commands;

public sealed record CreateAgencyCommand(string Name, string AgencyCode, string Description) : IRequest<JustinAgency>;

public class CreateAgencyCommandHandler : IRequestHandler<CreateAgencyCommand, JustinAgency>
{
    private readonly IAgencyService _agency;
    public CreateAgencyCommandHandler(IAgencyService agency)
    {
        _agency = agency;
    }

    public async Task<JustinAgency> Handle(CreateAgencyCommand request, CancellationToken cancellationToken)
    {
        var agency = new JustinAgency
        {
            Name = request.Name,
            AgencyCode = request.AgencyCode,
            Description = request.Description,
        };
        return await _agency.AddAgency(agency);
    }
}

