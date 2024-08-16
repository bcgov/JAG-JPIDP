namespace Pidp.Features.VersionInfo;

using System.Reflection;
using common.Constants.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidp.Infrastructure.Services;

[Route("api/[controller]")]
[Authorize(Policy = Policies.AnyPartyIdentityProvider)]

public class VersionController : PidpControllerBase
{

    public VersionController(IPidpAuthorizationService authService) : base(authService) { }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public string Index()
    {
        var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        return $"{version}";
    }
}
