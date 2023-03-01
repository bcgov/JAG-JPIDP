namespace edt.casemanagement.Features.Cases;

using MediatR;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CaseController : ControllerBase
{

    private readonly IMediator _mediator;

    public CaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{caseName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<CaseModel> SearchCases([FromRoute] string caseName)
    {
        return  await this._mediator.Send(new CaseLookupQuery(caseName));
    }

}
