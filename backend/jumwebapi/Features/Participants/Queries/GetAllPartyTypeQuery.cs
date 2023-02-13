using jumwebapi.Data.ef;
using jumwebapi.Features.Participants.Services;
using MediatR;

namespace jumwebapi.Features.Participants.Queries;
public record GetAllPartyTypeQuery : IRequest<IEnumerable<JustinPartyType>>;
public class GetAllPartyTypeQueryHandler : IRequestHandler<GetAllPartyTypeQuery, IEnumerable<JustinPartyType>>
{
    private readonly IPartyTypeService _partyTypeService;
    public GetAllPartyTypeQueryHandler(IPartyTypeService partyTypeService)
    {
        _partyTypeService = partyTypeService;
    }

    public async Task<IEnumerable<JustinPartyType>> Handle(GetAllPartyTypeQuery request, CancellationToken cancellationToken)
    {
        return await _partyTypeService.GetPartyTypeList();
    }
}

