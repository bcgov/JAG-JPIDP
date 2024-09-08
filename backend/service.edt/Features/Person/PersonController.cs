namespace edt.service.Features.Person;

using Common.Constants.Auth;
using Common.Models.EDT;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Policies.DiamInternalAuthentication)]
public class PersonController(IMediator mediator) : ControllerBase
{


    private readonly IMediator mediator = mediator;

    [HttpGet("{partyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EdtPersonDto>> GetUser([FromRoute] PersonQuery query)
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
    public async Task<ActionResult<EdtPersonDto>> GetUserByKey([FromRoute] PersonLookupModel lookupModel)
    {
        var search = new PersonSearchQuery(lookupModel);

        var c = await this.mediator.Send(search);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }


    [HttpGet("identifier/{identifierType}/{identifierValue}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EdtPersonDto>>> GetUsersByIdentifierAndType([FromRoute] PersonByIdentifierQuery query)
    {

        var c = await this.mediator.Send(query);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }


    /// <summary>
    /// This is strictly a get, but due to possible combinations of parameters its sent as a post
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EdtPersonDto>>> SearchForPerson([FromBody] PersonSearchQuery command)
    {

        var c = await this.mediator.Send(command);
        if (c == null)
        {
            return this.NotFound();
        }
        return this.Ok(c);
    }
}
