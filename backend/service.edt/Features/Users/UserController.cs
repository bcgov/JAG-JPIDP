namespace edt.service.Controllers;

using Common.Constants.Auth;
using Common.Models.EDT;
using edt.service.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Policies.DiamInternalAuthentication)]
public class UserController(IMediator mediator) : ControllerBase
{

    [HttpGet("party/{userKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EdtUserDto>> GetUser([FromServices] IRequestHandler<UserQuery, EdtUserDto> handler,
                                                                       [FromRoute] UserQuery query)
    {

        var c = await mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }

    [HttpGet("party/cases/{UserId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EdtUserDto>> GetUserCases([FromServices] IRequestHandler<UserCasesQuery, List<UserCaseSearchResponseModel>> handler,
                                                                     [FromRoute] UserCasesQuery query)
    {
        try
        {
            var c = await mediator.Send(query);
            if (c == null)
            {
                return this.NotFound();
            }
            return this.Ok(c);
        }
        catch (Exception ex)
        {
            // Return a 500 Internal Server Error response
            return this.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
