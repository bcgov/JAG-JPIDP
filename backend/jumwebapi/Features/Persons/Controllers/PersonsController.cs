using jumwebapi.Data.ef;
using jumwebapi.Features.Persons.Commands;
using jumwebapi.Features.Persons.Models;
using jumwebapi.Features.Persons.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace jumwebapi.Features.Persons.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PersonsController(IMediator mediator)
        {
            _mediator = mediator;   
        }
        [HttpGet]
        public async Task<IEnumerable<JustinPerson>> AllPerson()
        {
            return await _mediator.Send(new PersonQuery());
        }
        [HttpPost]
        public async Task<ActionResult<long>> CreatePerson([FromBody] Person person)
        {
            //var personCommand = new CreatePersonCommand
            var p = await _mediator.Send(request: new CreatePersonCommand(
                person.Surname,
                person.FirstName,
                person.MiddleNames,
                person.PreferredName,
                person.BirthDate
          ));
            return Ok(p);
        }
    }
}
