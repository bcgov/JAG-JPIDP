namespace edt.service.Features.Person;

using Common.Models.EDT;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PersonController : ControllerBase
{


    private readonly IMediator mediator;

    public PersonController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet("{partyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EdtPersonDto>> GetUser([FromServices] IRequestHandler<PersonQuery, EdtPersonDto> handler,
                                                                       [FromRoute] PersonQuery query)
    {

        var c = await this.mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }

    [HttpGet("key/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EdtPersonDto>> GetUserByKey([FromServices] IRequestHandler<PersonByKeyQuery, EdtPersonDto> handler,
                                                                   [FromRoute] PersonByKeyQuery query)
    {

        var c = await this.mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }


    [HttpGet("identifier/{identifierType}/{identifierValue}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EdtPersonDto>>> GetUsersByIdentifierAndType([FromServices] IRequestHandler<PersonByIdentifierQuery, List<EdtPersonDto>> handler,
                                                                   [FromRoute] PersonByIdentifierQuery query)
    {

        var c = await this.mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }
}
