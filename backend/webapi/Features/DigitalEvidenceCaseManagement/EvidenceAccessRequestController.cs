namespace Pidp.Features.DigitalEvidenceCaseManagement;

using Common.Constants.Auth;
using DomainResults.Common;
using DomainResults.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.DigitalEvidenceCaseManagement.Commands;
using Pidp.Features.DigitalEvidenceCaseManagement.Query;
using Pidp.Infrastructure.Services;
using Pidp.Models;

[Route("api/[controller]")]
public class EvidenceCaseManagementController : PidpControllerBase
{
    public EvidenceCaseManagementController(IPidpAuthorizationService authService) : base(authService) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("{requestId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DigitalEvidenceCaseModel?>> GetSubAgencyRequests([FromServices] IQueryHandler<DigitalEvidenceByRequestIdQuery.Query, DigitalEvidenceCaseModel?> handler,
                                                                                       [FromRoute] DigitalEvidenceByRequestIdQuery.Query query)
        => await handler.HandleAsync(new DigitalEvidenceByRequestIdQuery.Query(query.RequestId));


    [HttpGet("case/{caseId}")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DigitalEvidenceCaseModel?>> GetSubAgencyRequests([FromServices] IQueryHandler<DigitalEvidenceCaseByIdQuery.Query, DigitalEvidenceCaseModel?> handler,
                                                                                   [FromRoute] DigitalEvidenceCaseByIdQuery.Query query)
    => await handler.HandleAsync(new DigitalEvidenceCaseByIdQuery.Query(query.CaseId));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("getCaseUserKeys")]
    [Authorize(Roles = Roles.SubmittingAgencyClient)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> GetSubAgencyRequestsByCaseId([FromServices] IQueryHandler<SubmittingAgencyByCaseId.Query, List<string>> handler,
                                                                                   [FromQuery] SubmittingAgencyByCaseId.Query query)
    {
        var result = await handler.HandleAsync(new SubmittingAgencyByCaseId.Query(query.RCCNumber));
        return this.Ok(result);
    }


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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDigitalEvidenceSubAgencyCaseAccessRequest([FromServices] ICommandHandler<CaseAccessRequest.Command, IDomainResult> handler,
                                                      [FromBody] CaseAccessRequest.Command command)
        => await this.AuthorizePartyBeforeHandleAsync(command.PartyId, handler, command)
        .ToActionResult();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="command"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>

    [HttpGet("search")]
    [Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DigitalEvidenceCaseModel>> FindCase([FromServices] IQueryHandler<DigitalEvidenceCaseQuery.Query, DigitalEvidenceCaseModel> handler,
                                                                                                 [FromQuery] DigitalEvidenceCaseQuery.Query query)
    {

        try
        {
            var claim = this.HttpContext.User.Claims.First(c => c.Type == "preferred_username");
            var emailAddress = claim.Value;
            query.PartyId = emailAddress;
            var response = await handler.HandleAsync(query);
            if (response == null)
            {
                return this.NotFound();
            }
            if (response.Errors.Any())
            {
                return this.Problem(response.Errors);
            }
            return response;
        }
        catch (Exception ex)
        {
            return this.Problem(ex.Message);

        }



    }
}
