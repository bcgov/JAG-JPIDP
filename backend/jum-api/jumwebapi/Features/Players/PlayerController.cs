﻿using jumwebapi.Data.Security;
using jumwebapi.Features.Players.Commands;
using jumwebapi.Features.Players.Queries;
using jumwebapi.Models;
using jumwebapi.Policies;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace jumwebapi.Features.Players
{
    //[HasPermission(Permissions.AdminUsers)]
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PlayerController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        public async Task<IEnumerable<Player>> GetPlayers()
        {
            return await _mediator.Send(new GetAllPlayersQuery());
        }
        [HttpPost]
        public async Task<IActionResult> AddPlayer([FromBody] PlayerModel player)
        {
            var c =  await _mediator.Send(new CreatePlayerCommand(player.ShirtNo,player.Name,player.Apperance,player.Goals));

            return Ok(c);
        }
        [HttpPut("{playerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePlayer(int playerId, [FromBody] UpdatePlayerCommand request)
        {
            //if (playerId != request.Id) return BadRequest();

            await _mediator.Send( new UpdatePlayerCommand(request.Id,request.Name,request.Appearance,request.Goals, request.ShirtNo));
            return NoContent();

        }

    }
}
