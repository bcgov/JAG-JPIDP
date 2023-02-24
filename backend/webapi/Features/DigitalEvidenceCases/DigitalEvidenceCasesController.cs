namespace Pidp.Features.DigitalEvidenceCases;

using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;

[Route("api/[controller]")]
[Authorize(Policy = Policies.AnyPartyIdentityProvider)]
public class DigitalEvidenceCasesController : PidpControllerBase
{
    public DigitalEvidenceCasesController(IPidpAuthorizationService authService) : base(authService) { }

    [HttpGet("{caseName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Models.DigitalEvidenceCaseModel>> FindCase([FromServices] IQueryHandler<DigitalEvidenceCaseQuery.Query, Models.DigitalEvidenceCaseModel> handler,
                                                                                                 [FromRoute] DigitalEvidenceCaseQuery.Query query)
        => await handler.HandleAsync(query);


    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.AllDemsIdentityProvider)]
    //[Authorize(Policy = Policies.BcpsAuthentication)]
    public async Task<IActionResult> CreateCaseAccessRequest([FromServices] ICommandHandler<DigitalEvidenceCaseCommand.Command, IDomainResult> handler,
                                                              [FromBody] DigitalEvidenceCaseCommand.Command command)
    => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();

    //[HttpDelete]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[Authorize(Policy = Policies.AllDemsIdentityProvider)]
    //public async Task<IActionResult> DeleteCaseAccessRequest([FromServices] ICommandHandler<DigitalEvidenceCaseDelete.Command> handler)
    //{
    //}

}
