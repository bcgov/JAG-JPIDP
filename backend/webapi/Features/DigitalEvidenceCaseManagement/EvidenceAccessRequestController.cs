namespace Pidp.Features.DigitalEvidenceCaseManagement;

using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.DigitalEvidenceCaseManagement.Commands;
using Pidp.Features.DigitalEvidenceCaseManagement.Query;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;
using Pidp.Models;

[Route("api/[controller]")]
public class EvidenceCaseManagementController : PidpControllerBase
{
    public EvidenceCaseManagementController(IPidpAuthorizationService authService) : base(authService) { }

    [HttpGet("{requestId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DigitalEvidenceCaseModel?>> GetSubAgencyRequests([FromServices] IQueryHandler<Query.DigitalEvidenceByRequestIdQuery.Query, DigitalEvidenceCaseModel?> handler,
                                                                                       [FromRoute] Query.DigitalEvidenceByRequestIdQuery.Query query)
        => await handler.HandleAsync(new DigitalEvidenceByRequestIdQuery.Query(query.RequestId));

    [HttpGet("cases")]
    [Authorize(Roles = Roles.SubmittingAgencyClient)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> GetSubAgencyRequestsByCaseId([FromServices] IQueryHandler<SubmittingAgencyByCaseId.Query, List<string>> handler,
                                                                                   [FromQuery] SubmittingAgencyByCaseId.Query query)
    => await handler.HandleAsync(new SubmittingAgencyByCaseId.Query(query.RCCNumber));


    /// <summary>
    /// User needs to be in the sub agency IDP list (currently including IDIR for testing) and have the SUBMITTING_AGENCY role.
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider, Roles = Roles.SubmittingAgency)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<DigitalEvidenceCaseModel>>> GetSubAgencyRequestsByPartyId([FromServices] IQueryHandler<DigitalEvidenceByPartyQuery.Query, List<DigitalEvidenceCaseModel>> handler,
                                                              [FromQuery] DigitalEvidenceByPartyQuery.Query query)
    => await handler.HandleAsync(new DigitalEvidenceByPartyQuery.Query(query.PartyId));

    [HttpPost]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDigitalEvidenceSubAgencyCaseAccessRequest([FromServices] ICommandHandler<CaseAccessRequest.Command, IDomainResult> handler,
                                                      [FromBody] CaseAccessRequest.Command command)
        => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();

    [HttpPut("{requestId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveDigitalEvidenceSubAgencyCaseAccessRequest([FromServices] ICommandHandler<RemoveCaseAccess.Command, IDomainResult> handler,
                                            [FromRoute] RemoveCaseAccess.Command command)
    {
        await handler.HandleAsync(new RemoveCaseAccess.Command(command.RequestId));
        return this.NoContent();
    }


    [HttpGet("search")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DigitalEvidenceCaseModel>> FindCase([FromServices] IQueryHandler<DigitalEvidenceCaseQuery.Query, Models.DigitalEvidenceCaseModel> handler,
                                                                                                 [FromQuery] DigitalEvidenceCaseQuery.Query query)
        => await handler.HandleAsync(query);
}
