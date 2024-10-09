namespace ApprovalFlow.Features.Approvals;

using Common.Constants.Auth;
using Common.Models.Approval;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

[Route("api/[controller]")]
[ApiController]
public class ApprovalsController(IMediator mediator) : ControllerBase
{


    private static readonly Histogram ApprovalLookupDuration = Metrics.CreateHistogram("approval_lookup_duration", "Histogram of approval searches.");

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Policies.ApprovalAuthorization)]

    public async Task<ActionResult<IList<ApprovalModel>>> GetApprovals([FromQuery] bool pending)
    {
        using (ApprovalLookupDuration.NewTimer())
        {
            var response = await mediator.Send(new ApprovalsQuery(pending));
            return this.Ok(response);
        }
    }


    [HttpPost("response")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Policies.ApprovalAuthorization)]

    public async Task<ActionResult<ApprovalModel>> PostApprovalResponse([FromBody] ApprovalResponseInput command)
    {

        var response = await mediator.Send(command);

        return response;
    }
}
