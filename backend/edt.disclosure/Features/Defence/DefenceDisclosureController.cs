namespace edt.disclosure.Features.Defence;

using Common.Models.EDT;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

[Route("api/[controller]")]
[ApiController]
public class DefenceDisclosureController : ControllerBase
{
    private readonly IMediator _mediator;

    private static readonly Histogram FolioFindDuration = Metrics.CreateHistogram("folio_lookup_duration", "Histogram of folio searches.");
    private static readonly Histogram CaseSearchDuration = Metrics.CreateHistogram("case_search_duration", "Histogram of case searches.");

    public DefenceDisclosureController(IMediator mediator)
    {
        this._mediator = mediator;
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
                return this.NotFound();
            }
            else
            {
                return this.Ok(response);
            }
        }
    }

    [HttpGet("case/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseModel>> FindCaseByKey([FromRoute] string key)
    {
        using (CaseSearchDuration.NewTimer())
        {

            var response = await this._mediator.Send(new CaseKeyQuery(key, true));
            if (response == null)
            {
                return this.NotFound();
            }
            else
            {
                return this.Ok(response);
            }
        }
    }

    [HttpGet("case/summary/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseModel>> FindCaseSummaryByKey([FromRoute] string key)
    {
        using (CaseSearchDuration.NewTimer())
        {

            var response = await this._mediator.Send(new CaseKeyQuery(key, false));
            if (response == null)
            {
                return this.NotFound();
            }
            else
            {
                return this.Ok(response);
            }
        }
    }
}
