namespace Pidp.Features.Admin.SubmittingAgencies;

using Common.Constants.Auth;
using DomainResults.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Services;
using Pidp.Models;

[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminAuthentication)]
public class SubmittingAgenciesController : PidpControllerBase
{

    public SubmittingAgenciesController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubmittingAgencyModel>>> GetSubmittingAgencies([FromServices] IQueryHandler<SubmittingAgencyQuery, List<SubmittingAgencyModel>> handler,
                                                                   [FromQuery] SubmittingAgencyQuery query)
    => await handler.HandleAsync(query);

    [HttpPut()]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IDomainResult<SubmittingAgencyModel>> Update([FromServices] ICommandHandler<UpdateSubmittingAgencyCommand.Command, IDomainResult<SubmittingAgencyModel>> handler,
                                            [FromBody] UpdateSubmittingAgencyCommand.Command command)
        => await handler.HandleAsync(command);


}
