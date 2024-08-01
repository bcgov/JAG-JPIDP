namespace ApprovalFlow.Features.Approvals;

using Common.Constants.Auth;
using Common.Models.Approval;
using DomainResults.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

[Route("api/[controller]")]
[ApiController]
public class ApprovalsController : ControllerBase
{


    private readonly IMediator _mediator;
    private static readonly Histogram ApprovalLookupDuration = Metrics.CreateHistogram("approval_lookup_duration", "Histogram of approval searches.");

    public ApprovalsController(IMediator mediator) => this._mediator = mediator;

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Policies.ApprovalAuthorization)]

    public async Task<ActionResult<IList<ApprovalModel>>> GetPendingApprovals([FromQuery] bool pendingOnly)
    {
        using (ApprovalLookupDuration.NewTimer())
        {
            var response = await this._mediator.Send(new ApprovalsQuery(pendingOnly));
            return this.Ok(response);
        }
    }

    [HttpPost("response")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Policies.ApprovalAuthorization)]

    public async Task<ActionResult<ApprovalModel>> PostApprovalResponse([FromBody] ApproveDenyInput command)
    {
        var user = HttpContext.User.Identities.First().Claims.FirstOrDefault( claim => claim.Type.Equals(Claims.PreferredUsername))?.Value;
        command.ApproverUserId = user;
        var response = this._mediator.Send(command).Result;

        return response;
    }
}
