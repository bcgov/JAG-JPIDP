namespace Pidp.Features.DefenceCounsel;


using Common.Constants.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features.DefenceCounsel.Query;
using Pidp.Infrastructure.Services;
using Pidp.Models;

[Route("api/[controller]")]
public class DefenceCounselController : PidpControllerBase
{
    public DefenceCounselController(IPidpAuthorizationService authService) : base(authService) { }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("{partyId}/{defenceUniqueID}")]
    [Authorize(Policy = Policies.VerifiedCredentialsProvider)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DigitalEvidenceCaseModel?>> GetSubAgencyRequests([FromServices] IQueryHandler<DefenceFolioQuery.Query, DigitalEvidenceCaseModel?> handler,
                                                                                       [FromRoute] DefenceFolioQuery.Query query)
        => await handler.HandleAsync(new DefenceFolioQuery.Query(query.PartyId, query.DefenceUniqueID));


}
