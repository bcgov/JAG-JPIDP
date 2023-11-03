using jumwebapi.Data.ef;
using jumwebapi.Data.Security;
using jumwebapi.Features.Participants.Queries;
using jumwebapi.Policies;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace jumwebapi.Features.Participants.Controllers;

[HasPermission(Permissions.AdminUsers)]
[Route("api/[controller]")]
[ApiController]
public class PartyTypeController : ControllerBase
{
    private readonly IMediator _mediator;
    public PartyTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<IEnumerable<JustinPartyType>> GetpartyType()
    {
        return await _mediator.Send(new GetAllPartyTypeQuery());
    }
    
}
