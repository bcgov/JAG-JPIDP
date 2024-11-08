namespace edt.service.Features.Participant;

using Common.Constants.Auth;
using CommonModels.Models.Party;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Policies.DiamInternalAuthentication)]
public class ParticipantController(IMediator mediator, ILogger<ParticipantController> logger) : ControllerBase
{
    [HttpGet("merge-details/{PartId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantMergeListingModel>> GetMergeParticipantDetails([FromRoute] ParticipantByPartId query)
    {
        logger.LogInformation($"New request to GetMergeParticipantDetails({query})");

        var c = await mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }
}
