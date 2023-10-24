namespace edt.service.Infrastructure.Auth;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

public class RealmAccessRoleHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {

        if (!context.User.Claims.Any())
        {
            return Task.CompletedTask;
        }
        var claims = context.User.Claims.First(c => c.Type.Equals(Claims.ResourceAccess));
        if (claims.Value.Equals("DIAM-INTERNAL"))
        {
            Serilog.Log.Information($"Claim {claims.Value}");
        }
        return Task.CompletedTask;
    }


}

