namespace ApprovalFlow.Features.Approvals;

using common.Constants.Auth;
using Common.Models.Approval;
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

    public async Task<ActionResult<IList<ApprovalModel>>> GetPendingApprovals()
    {
        using (ApprovalLookupDuration.NewTimer())
        {
            var response = await this._mediator.Send(new PendingApprovalsQuery());

            return this.Ok(response);

        }
    }
}
