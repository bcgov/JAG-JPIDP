namespace Pidp.Features.Admin.SubmittingAgencies;

using DomainResults.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.DigitalEvidenceCaseManagement.Commands;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;
using Pidp.Models.Lookups;

[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminAuthentication)]
public class SubmittingAgenciesController : PidpControllerBase
{

    public SubmittingAgenciesController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubmittingAgency>>> GetSubmittingAgencies([FromServices] IQueryHandler<SubmittingAgencyQuery, List<SubmittingAgency>> handler,
                                                                   [FromQuery] SubmittingAgencyQuery query)
    => await handler.HandleAsync(query);

    [HttpPut("{agencyCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IDomainResult> Update([FromServices] ICommandHandler<UpdateSubmittingAgencyCommand.Command, IDomainResult> handler,
                                            [FromRoute] UpdateSubmittingAgencyCommand.Command command)
        => await handler.HandleAsync(command);


}
