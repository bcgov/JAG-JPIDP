namespace Pidp.Features.Admin.IdentityProviders;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;

[Route("api/[controller]")]
[Authorize(Policy = Policies.AdminAuthentication)]
public class IdentityProviderController : PidpControllerBase
{

    public IdentityProviderController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PartyIndex.Model>>> GetIdentityProviders([FromServices] IQueryHandler<PartyIndex.Query, List<PartyIndex.Model>> handler,
                                                                       [FromQuery] PartyIndex.Query query)
        => await handler.HandleAsync(query);
}
