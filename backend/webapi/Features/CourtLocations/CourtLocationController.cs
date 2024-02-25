namespace Pidp.Features.CourtLocations;

using common.Constants.Auth;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.CourtLocations.Commands;
using Pidp.Features.CourtLocations.Query;
using Pidp.Infrastructure.Services;
using Pidp.Models;
using Pidp.Models.Lookups;

[Route("api/[controller]")]
[Authorize(Policy = Policies.VerifiedCredentialsProvider)]
public class CourtLocationController : PidpControllerBase
{
    public CourtLocationController(IPidpAuthorizationService authService) : base(authService) { }


    /// <summary>
    /// Get all available court locations
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CourtLocation>>> GetCourtLocations([FromServices] IQueryHandler<CourtLocationQuery.Query, List<CourtLocation>> handler,
                                                                       [FromQuery] CourtLocationQuery.Query query)
    {
        var result = await handler.HandleAsync(query);
        return this.Ok(result);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPut("{requestId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCourtLocationRequest([FromServices] ICommandHandler<UpdateCourtLocationAccessRequest.Command, IDomainResult> handler,
                                            [FromRoute] UpdateCourtLocationAccessRequest.Command command)
    {
        return this.NoContent();
    }


    [HttpDelete("party/{partyId}/request/{requestId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCourtLocationRequest([FromServices] ICommandHandler<RemoveCourtLocationRequest.Command, IDomainResult> handler,
                                        [FromRoute] RemoveCourtLocationRequest.Command command)

          => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();


    /// <summary>
    /// Request access to court location
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDigitalEvidenceCourtLocationRequest([FromServices] ICommandHandler<CourtAccessRequest.Command, IDomainResult> handler,
                                                      [FromBody] CourtAccessRequest.Command command)
        => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();

    /// <summary>
    /// Get requests by party Id
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("party/{partyId}/requests")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CourtLocationAccessModel>>> GetCourtLocationRequestsByParty(
    [FromRoute(Name = "partyId")] int partyId,
    [FromQuery(Name = "includeDeleted")] bool includeDeleted,
    [FromServices] IQueryHandler<CourtLocationRequestsByPartyQuery.Query, List<CourtLocationAccessModel>> handler)
    {
        var query = new CourtLocationRequestsByPartyQuery.Query(partyId, includeDeleted);

        var result = await handler.HandleAsync(query);

        if (result == null || result.Count == 0)
        {
            return this.NoContent();
        }

        return this.Ok(result);
    }


}
