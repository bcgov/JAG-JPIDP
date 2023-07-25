namespace edt.casemanagement.Features.Cases;

using Common.Models.EDT;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

[Route("api/[controller]")]
[ApiController]
public class CaseController : ControllerBase
{

    private readonly IMediator _mediator;
    private static readonly Histogram CaseFindDuration = Metrics
        .CreateHistogram("case_search_duration", "Histogram of case searches.");
    //public CaseController(IMediator mediator, IEdtAuthorizationService authService) : base(authService)
    //{
    //    _mediator = mediator;
    //}

    public CaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("id/{caseId}")]
    //[Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseModel>> GetCase([FromRoute] int caseId)
    {
        using (CaseFindDuration.NewTimer())
        {

            var response = await this._mediator.Send(new CaseGetByIdQuery(caseId));
            if (response == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(response);
            }
        }
    }


    [HttpGet("{caseName}")]
    //[Authorize(Policy = Policies.SubAgencyIdentityProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseModel>> SearchCases([FromRoute] string caseName)
    {
        using (CaseFindDuration.NewTimer())
        {

            var response = await this._mediator.Send(new CaseLookupQuery(caseName));
            if (response == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(response);
            }
        }
    }

}
