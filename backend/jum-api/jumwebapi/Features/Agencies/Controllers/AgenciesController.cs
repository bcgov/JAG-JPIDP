using jumwebapi.Data.ef;
using jumwebapi.Features.Agencies.Commands;
using jumwebapi.Features.Agencies.Models;
using jumwebapi.Features.Agencies.Queries;
using jumwebapi.Infrastructure.HttpClients.Keycloak;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace jumwebapi.Features.Agencies.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgenciesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IKeycloakAdministrationClient _keycloak;
    public AgenciesController(IMediator mediator, IKeycloakAdministrationClient keycloak)
    {
        _mediator = mediator;
        _keycloak = keycloak;
    }
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IEnumerable<JustinAgency>> Get()
    {
        //var u = await _keycloak.IdentityProviders();
        return await _mediator.Send(new AgencyQuery());
    }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] Agency agency)
    {
        var c = await _mediator.Send(new CreateAgencyCommand(agency.Name, agency.AgencyCode, agency.Description));
        return Ok(c);
    }
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] JustinAgency agency, long AgencyId)
    {
        var a = await _mediator.Send(new UpdateAgencyCommand(AgencyId, agency.Name, agency.AgencyCode, agency.Description));
        return Ok(a);
    }
}
