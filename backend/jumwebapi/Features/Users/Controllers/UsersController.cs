using jumwebapi.Features.Users.Commands;
using jumwebapi.Features.Users.Models;
using jumwebapi.Features.Users.Queries;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Kafka.Constants;
using jumwebapi.Kafka.Producer.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace jumwebapi.Features.Users.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IKafkaProducer<string, UserModel> _kafkaProducer;
    private readonly jumwebapiConfiguration _config;
    public UsersController(IMediator mediator, IKafkaProducer<string, UserModel> kafkaProducer, jumwebapiConfiguration config)
    {
        _mediator = mediator;
        _kafkaProducer = kafkaProducer;
        _config = config;
    }
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers()
    {
        var e =  await _mediator.Send(new AllUsersQuery());
        return new JsonResult(e);
    }

    [HttpGet("{username:alpha}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string username)
    {
        var user = await _mediator.Send(new GetUserQuery(username));
        return new JsonResult(user);
    }
    [HttpGet("{partId:long}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUser(decimal partId)
    {
        var user = await _mediator.Send(new GetUserByPartId(partId));
        return new JsonResult(user);
    }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUser([FromBody] UserModel user)
    {
        var entity = await _mediator.Send(new CreateUserCommand(
            user.UserName, user.ParticipantId, user.IsDisable, user.FirstName, user.LastName, user.MiddleName,user.PreferredName, user.PhoneNumber, user.Email, user.BirthDate,
            user.AgencyId, user.PartyTypeCode, user.Roles         
            ));

        // place a message on the notification topic
        await _kafkaProducer.ProduceAsync(_config.KafkaCluster.TopicName, user.ParticipantId.ToString(), entity);
        return Ok(entity);
    }
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(long ParticipantId, [FromBody] UserModel user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var update = await _mediator.Send(new UpdateUserCommand(
                user.UserName, user.ParticipantId, user.IsDisable,
                user.FirstName, user.LastName, user.MiddleName, user.PreferredName,
                user.PhoneNumber, user.Email, user.BirthDate, user.AgencyId,
                user.PartyTypeCode, user.Roles
            ));

        return Ok(update);
    }


}
