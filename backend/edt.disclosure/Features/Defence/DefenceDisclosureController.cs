namespace edt.disclosure.Features.Defence;

using edt.disclosure.Features.Cases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

[Route("api/[controller]")]
[ApiController]
public class DefenceDisclosureController : ControllerBase
{
    private readonly IMediator _mediator;

    private static readonly Histogram FolioFindDuration = Metrics
    .CreateHistogram("folio_search_duration", "Histogram of folio searches.");

    public DefenceDisclosureController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet("folio/{folioID}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseModel>> FindCaseByFolio([FromRoute] string folioID)
    {
        using (FolioFindDuration.NewTimer())
        {

            var response = await this._mediator.Send(new CaseQuery("Folio ID", folioID));
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
