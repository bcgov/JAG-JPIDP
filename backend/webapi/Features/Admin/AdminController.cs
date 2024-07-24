namespace Pidp.Features.Admin;

using common.Constants.Auth;
using Common.Models.AccessRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.Admin.AccessRequests;
using Pidp.Infrastructure.Services;

[Route("api/[controller]")]
//[Authorize(Policy = Policies.AdminAuthentication)]
[Authorize(Policy = Policies.AdminClientAuthentication)]
public class AdminController : PidpControllerBase
{
    public AdminController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpDelete("parties")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteParties([FromServices] ICommandHandler<PartyDelete.Command> handler)
    {
        if (PidpConfiguration.IsProduction())
        {
            return this.Forbid();
        }

        await handler.HandleAsync(new PartyDelete.Command());
        return this.NoContent();
    }

    [HttpGet("party/{partyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartyModel>> GetParty([FromServices] IQueryHandler<PartyDetailQuery, PartyModel> handler,
                                                                       [FromRoute] PartyDetailQuery query)
    {

        var userInfo = await handler.HandleAsync(query);

        if (userInfo != null)
        {
            return userInfo;
        }
        else
        {
            return this.NotFound();
        }
    }

    [HttpGet("party/requests/{userName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AccessRequestDTO>>> GetUserAccessRequests([FromServices] IQueryHandler<AccessRequestQuery, List<AccessRequestDTO>> handler,
                                                                   [FromRoute] AccessRequestQuery query)
    {

        var userAccessRequests = await handler.HandleAsync(query);

        if (userAccessRequests != null)
        {
            return userAccessRequests;
        }
        else
        {
            return this.NotFound();
        }
    }


    [HttpDelete("party/{partyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartyModel>> ResetParty([FromServices] IQueryHandler<PartyDetailQuery, PartyModel> handler,
                                                                    [FromRoute] PartyDetailQuery query)
    {

        var userInfo = await handler.HandleAsync(query);

        if (userInfo != null)
        {
            return userInfo;
        }
        else
        {
            return this.NotFound();
        }
    }


    [HttpGet("parties")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PartyIndex.Model>>> GetParties([FromServices] IQueryHandler<PartyIndex.Query, List<PartyIndex.Model>> handler,
                                                                       [FromQuery] PartyIndex.Query query)
        => await handler.HandleAsync(query);
}
