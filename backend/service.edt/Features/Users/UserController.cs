namespace edt.service.Controllers;

using Common.Models.EDT;
using edt.service.Features.Users;
using edt.service.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Policies.AdminAuthentication)]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("party/{partyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EdtUserDto>> GetUser([FromServices] IRequestHandler<UserQuery, EdtUserDto> handler,
                                                                       [FromRoute] UserQuery query)
    {

        var c = await this._mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }

}
