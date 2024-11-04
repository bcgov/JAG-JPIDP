namespace jumwebapi.Features.Users.Controllers;

using global::Common.Kafka;
using jumwebapi.Features.Users.Commands;
using jumwebapi.Features.Users.Models;
using jumwebapi.Features.Users.Queries;
using jumwebapi.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IKafkaProducer<string, UserModel> _kafkaProducer;
    private readonly JumWebApiConfiguration _config;
    public UsersController(IMediator mediator, IKafkaProducer<string, UserModel> kafkaProducer, JumWebApiConfiguration config)
    {
        this._mediator = mediator;
        this._kafkaProducer = kafkaProducer;
        this._config = config;
    }
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers()
    {
        var e = await this._mediator.Send(new AllUsersQuery());
        return new JsonResult(e);
    }

    [HttpGet("username/{username:alpha}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string username)
    {
        var user = await this._mediator.Send(new GetUserQuery(username));
        return new JsonResult(user);
    }
    [HttpGet("partid/{partId:long}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUser(decimal partId)
    {
        var user = await this._mediator.Send(new GetUserByPartId(partId));
        return new JsonResult(user);
    }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUser([FromBody] UserModel user)
    {
        var entity = await this._mediator.Send(new CreateUserCommand(
            user.UserName, user.ParticipantId, user.IsDisable, user.FirstName, user.LastName, user.MiddleName, user.PreferredName, user.PhoneNumber, user.Email, user.BirthDate,
            user.AgencyId, user.PartyTypeCode, user.Roles
            ));

        await this._kafkaProducer.ProduceAsync(this._config.KafkaCluster.TopicName, user.ParticipantId.ToString(), entity);
        return this.Ok(entity);
    }
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(long ParticipantId, [FromBody] UserModel user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var update = await this._mediator.Send(new UpdateUserCommand(
                user.UserName, user.ParticipantId, user.IsDisable,
                user.FirstName, user.LastName, user.MiddleName, user.PreferredName,
                user.PhoneNumber, user.Email, user.BirthDate, user.AgencyId,
                user.PartyTypeCode, user.Roles
            ));

        return this.Ok(update);
    }


}
