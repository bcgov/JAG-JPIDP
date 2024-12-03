namespace jumwebapi.Features.UserChangeManagement.Controllers;

using jumwebapi.Features.UserChangeManagement.Commands;
using jumwebapi.Features.UserChangeManagement.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserChangeManagementController : ControllerBase
{

    private readonly IMediator _mediator;

    public UserChangeManagementController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserProcessFlag([FromBody] JustinProcessStatusModel changeData)
    {
        if (changeData == null)
            throw new ArgumentNullException(nameof(changeData));

        var success = await this._mediator.Send(new UpdateJustinUserUpdateStatusCommand(changeData));
        if (success)
        {
            return this.Ok();
        }
        else
        {
            return this.BadRequest($"Failed to update {changeData.EventMessageId}");
        }

    }
}
