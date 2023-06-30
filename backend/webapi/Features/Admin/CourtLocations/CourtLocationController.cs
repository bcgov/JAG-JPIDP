namespace Pidp.Features.Admin.CourtLocations;

using DomainResults.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;
using Pidp.Models.Lookups;

[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminAuthentication)]
public class CourtLocationController : PidpControllerBase
{
    public CourtLocationController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourtLocation>>> GetSubmittingAgencies([FromServices] IQueryHandler<CourtLocationQuery, List<CourtLocation>> handler,
                                                               [FromQuery] CourtLocationQuery query)
=> await handler.HandleAsync(query);


    [HttpPut()]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IDomainResult<CourtLocation>> Update([FromServices] ICommandHandler<UpdateCourtLocationCommand.Command, IDomainResult<CourtLocation>> handler,
                                            [FromBody] UpdateCourtLocationCommand.Command command)
        => await handler.HandleAsync(command);
}
