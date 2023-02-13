using jumwebapi.Features.Participants.Models;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using jumwebapi.Features.Participants.Queries;
using Serilog;
using System.Text.Json;

namespace jumwebapi.Features.Participants.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ParticipantController : ControllerBase
{
    private readonly IMediator _mediator;

    public ParticipantController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ParticipantByUsername([Required]string username)
    {
        Log.Logger.Information("Getting participant info for {0}", username);
        var participant = await _mediator.Send(new GetParticipantByUsernameQuery(username));
        Log.Logger.Information("Got {0}", JsonSerializer.Serialize(participant));

        return new JsonResult(participant);
    }
    [HttpGet("{partId:decimal}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ParticipantById(decimal partId)
    {
        var participant = await _mediator.Send(new GetParticipantByIdQuery(partId));
        return new JsonResult(participant);
    }
}

