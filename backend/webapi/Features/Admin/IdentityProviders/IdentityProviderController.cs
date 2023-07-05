namespace Pidp.Features.Admin.IdentityProviders;

using DomainResults.Common;
using Keycloak.Net.Models.RealmsAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Auth;
using Pidp.Infrastructure.Services;

[Route("api/admin/[controller]")]
[Authorize(Policy = Infrastructure.Auth.Policies.AdminAuthentication)]
public class IdentityProviderController : PidpControllerBase
{

    public IdentityProviderController(IPidpAuthorizationService authorizationService) : base(authorizationService) { }

    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IdentityProvider>>> GetIdentityProviders([FromServices] IQueryHandler<IdentityProviderQuery, List<IdentityProvider>> handler,
                                                                       [FromQuery] IdentityProviderQuery query)
        => await handler.HandleAsync(query);
}
