namespace jumwebapi.Features.Cases.Controllers;

using global::Common.Models.JUSTIN;
using jumwebapi.Features.Cases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class JustinCaseController : ControllerBase
{
    private readonly IMediator mediator;

    public JustinCaseController(IMediator _mediator) => this.mediator = _mediator;

    [HttpGet("{caseId}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaseStatusRequest([Required] string caseId)
    {
        Log.Logger.Information($"Getting case status for {caseId}");
        var caseStatus = await this.mediator.Send(new GetCaseStatusQuery(caseId));
        if (caseStatus != null && caseStatus == CaseStatus.NotFound)
        {
            return this.NotFound();
        }
        return this.Ok(caseStatus);
    }
}
