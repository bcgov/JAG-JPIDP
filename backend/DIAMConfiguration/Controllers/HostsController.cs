namespace DIAMConfiguration.Controllers;

using Common.Utils;
using DIAMConfiguration.Data;
using DIAMConfiguration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



[Route("api/config/user-group")]
[ApiController]
public class HostsController(DIAMConfigurationDataStoreDbContext context) : ControllerBase
{
    private const bool PREFER_REFERER = true;

    // get login options
    [HttpGet("login-options")]
    public async Task<ActionResult<IEnumerable<LoginOption>>> GetLoginOptions()
    {

        var request = this.HttpContext.Request;
        var hostname = HttpUtils.GetHostFromHeader(request, PREFER_REFERER);

        if (string.IsNullOrEmpty(hostname))
        {
            Serilog.Log.Warning($"Unable to determine host from request");
            return this.NotFound();
        }


        Serilog.Log.Information($"Getting options for {hostname}");

        var res = await context.HostConfigs.AsSplitQuery().Where(host => host.Hostname == hostname).Include(host => host.HostLoginConfigs).Where(config => config.Deleted == null).Select(config => LoginOption.FromHostConfig(config)).FirstOrDefaultAsync();

        if (res != null && res.Any())
        {
            return this.Ok(res);
        }
        else
        {
            Serilog.Log.Warning($"Host not defined [{hostname}]");
            return this.NotFound();

        }


    }

}

