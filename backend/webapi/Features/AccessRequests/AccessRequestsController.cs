namespace Pidp.Features.AccessRequests;

using common.Constants.Auth;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Services;
using Pidp.Models;

[Route("api/[controller]")]
public class AccessRequestsController : PidpControllerBase
{
    public AccessRequestsController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet("{partyId}")]
    [Authorize(Policy = Policies.AnyPartyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Index.Model>>> GetAccessRequests([FromServices] IQueryHandler<Index.Query, List<Index.Model>> handler,
                                                                         [FromRoute] Index.Query query)
        => await this.AuthorizePartyBeforeHandleAsync(query.PartyId, handler, query)
            .ToActionResultOfT();

    [HttpGet("digital-evidence/validate/{partyId}/{code}")]
    [Authorize(Policy = Policies.BcscAuthentication)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidatePublicUserCode([FromServices] ICommandHandler<ValidateUser.Command, IDomainResult<UserValidationResponse>> handler,
                                                          [FromRoute] ValidateUser.Command command)
    {

        var remoteIpAddress = this.Request.HttpContext.Connection.RemoteIpAddress;
        command.IPAddress = remoteIpAddress;
        return await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)

             .ToActionResult();
    }


    [HttpPost("driver-fitness")]
    [Authorize(Policy = Policies.BcscAuthentication)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDriverFitnessEnrolment([FromServices] ICommandHandler<DriverFitness.Command, IDomainResult> handler,
                                                                  [FromBody] DriverFitness.Command command)
        => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
            .ToActionResult();

    [HttpPost("digital-evidence")]
    [Authorize(Policy = Policies.AllDemsIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDigitalEvidenceEnrolment([FromServices] ICommandHandler<DigitalEvidence.Command, IDomainResult> handler,
                                                              [FromBody] DigitalEvidence.Command command)
    => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();

    [HttpPost("digital-evidence-defence")]
    [Authorize(Policy = Policies.AllDemsIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDigitalEvidenceDefenceEnrolment([FromServices] ICommandHandler<DigitalEvidenceDefence.Command, IDomainResult> handler,
                                                          [FromBody] DigitalEvidenceDefence.Command command)
     => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();


    [HttpPost("digital-evidence-disclosure")]
    [Authorize(Policy = Policies.BcscAuthentication)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePublicDigitalEvidenceDisclosureEnrolment([FromServices] ICommandHandler<DigitalEvidencePublicDisclosure.Command, IDomainResult> handler,
                                                      [FromBody] DigitalEvidencePublicDisclosure.Command command)
 => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
    .ToActionResult();


    /// <summary>
    /// Get all public folios in disclosure user has successfully requested access to
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("digital-evidence-disclosure/cases/{partyId}")]
    [Authorize(Policy = Policies.BcscAuthentication)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PublicDisclosureAccess>>> GetDisclosureCaseListing([FromServices] IQueryHandler<PublicDisclosureAccessQuery.Query, List<PublicDisclosureAccess>> handler,
                                                                         [FromRoute] PublicDisclosureAccessQuery.Query query)
        => await this.AuthorizePartyBeforeHandleAsync(query.PartyId, handler, query)
            .ToActionResultOfT();



}
