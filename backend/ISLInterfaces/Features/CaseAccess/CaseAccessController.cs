namespace ISLInterfaces.Features.CaseAccess;

using System.ComponentModel.DataAnnotations;
using ISLInterfaces.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/[controller]")]
public class CaseAccessController : ControllerBase
{
    private readonly ILogger<CaseAccessController> _logger;
    private ICaseAccessService caseAccessService;

    public CaseAccessController(ILogger<CaseAccessController> logger, ICaseAccessService caseAccessService)
    {
        _logger = logger;
        this.caseAccessService = caseAccessService;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("getCaseUserKeys")]
    [Authorize(Roles = Roles.SubmittingAgencyClient)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // CQRS is redundant for this as we're not writing and there's no separation of data concerns
    // between reads and writes anyway
    public async Task<ActionResult<List<string>>> GetSubAgencyRequestsByCaseId([FromQuery][Required] string RCCNumber)
    {
        this._logger.LogInformation($"Case request {RCCNumber}");
        return await this.caseAccessService.GetCaseAccessUsersAsync(RCCNumber);
    }



}
