namespace jumwebapi.Features.Participants.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using jumwebapi.Features.Participants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;



[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ParticipantController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ParticipantByUsername([Required] string username)
    {
        Log.Logger.Information("Getting participant info for {0}", username);
        var participant = await mediator.Send(new GetParticipantByUsernameQuery(username));
        Log.Logger.Information("Got participant {0}", JsonSerializer.Serialize(participant));
        return new JsonResult(participant);
    }

    [HttpGet("{partId:decimal}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ParticipantById(decimal partId)
    {
        var participant = await mediator.Send(new GetParticipantByIdQuery(partId));
        return new JsonResult(participant);
    }
}

