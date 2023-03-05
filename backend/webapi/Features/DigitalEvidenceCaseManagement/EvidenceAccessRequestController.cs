namespace Pidp.Features.DigitalEvidenceCaseManagement;

using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.DigitalEvidenceCaseManagement.Commands;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;

[Route("api/[controller]")]
public class EvidenceAccessRequestController : PidpControllerBase
{
    public EvidenceAccessRequestController(IPidpAuthorizationService authService) : base(authService) { }

    [HttpGet("requests/{requestId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Query.SubmittingAgency.Model?>> GetSubAgencyRequests([FromServices] IQueryHandler<Query.SubmittingAgency.Query, Query.SubmittingAgency.Model?> handler,
                                                                                       [FromRoute] Query.SubmittingAgency.Query query)
        => await handler.HandleAsync(new Query.SubmittingAgency.Query(query.RequestId));

    [HttpGet("cases/{caseNumber}")]
    [Authorize(Roles = Roles.SubmittingAgencyClient)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Query.SubmittingAgencyByCaseId.Model>>> GetSubAgencyRequestsByCaseId([FromServices] IQueryHandler<Query.SubmittingAgencyByCaseId.Query, List<Query.SubmittingAgencyByCaseId.Model>> handler,
                                                                                   [FromRoute] Query.SubmittingAgencyByCaseId.Query query)
    => await handler.HandleAsync(new Query.SubmittingAgencyByCaseId.Query(query.CaseNumber));

    [HttpGet("parties/{partyId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider, Roles = Roles.SubmittingAgency)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Query.SubmittingAgencyByPartyId.Model>>> GetSubAgencyRequestsByPartyId([FromServices] IQueryHandler<Query.SubmittingAgencyByPartyId.Query, List<Query.SubmittingAgencyByPartyId.Model>> handler,
                                                              [FromRoute] Query.SubmittingAgencyByPartyId.Query query)
    => await handler.HandleAsync(new Query.SubmittingAgencyByPartyId.Query(query.PartyId));

    [HttpPost]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDigitalEvidenceSubAgencyEnrolment([FromServices] ICommandHandler<SubmittingAgency.Command, IDomainResult> handler,
                                                      [FromBody] SubmittingAgency.Command command)
        => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();

    [HttpDelete("requests/{requestId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider, Roles = Roles.SubmittingAgency)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveDigitalEvidenceSubAgencyEnrolment([FromServices] ICommandHandler<RemoveCaseAccess.Command, IDomainResult> handler,
                                            [FromRoute] RemoveCaseAccess.Command command)
    {
        await handler.HandleAsync(new RemoveCaseAccess.Command(command.RequestId));
        return this.NoContent();
    }
}
