using DIAMConfiguration.Data;
using DIAMConfiguration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIAMConfiguration.Controllers
{
    [Route("api/config/user-group")]
    [ApiController]
    public class HostsController : ControllerBase
    {
        private readonly DIAMConfigurationDataStoreDbContext _context;

        public HostsController(DIAMConfigurationDataStoreDbContext context)
        {
            this._context = context;
        }

        // get login options
        [HttpGet("login-options")]
        public async Task<ActionResult<IEnumerable<LoginOption>>> GetLoginOptions()
        {

            var request = this.HttpContext.Request;
            var headers = request.Headers;
            var hasHost = headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Host, out var hostName);
            var hasReferer = headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Referer, out var referer);

            if (hasHost || hasReferer)
            {
                var hostname = hostName.FirstOrDefault();
                Serilog.Log.Information($"Getting host Host={hostName} Referer={referer}");
                if (hasHost)
                {
                    hostname = hostname.Contains(":") ? hostname.Split(":")[0] : hostname;
                }
                else
                {
                    hostname = referer.FirstOrDefault();
                    hostname = new Uri(referer).Host;
                }
                var res = await this._context.HostConfigs.AsSplitQuery().Where(host => host.Hostname == hostname).Include(host => host.HostLoginConfigs).Where(config => config.Deleted == null).Select(config => LoginOption.FromHostConfig(config)).FirstOrDefaultAsync();

                return this.Ok(res);
            }
            else
            {
                return Array.Empty<LoginOption>();
            }

        }

        // GET: api/LoginConfigHosts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HostConfig>>> GetLoginConfigHosts()
        {

            var request = this.HttpContext.Request;
            var headers = request.Headers;
            var hasHost = headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Host, out var hostName);
            var hasReferer = headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Referer, out var referer);

            if (hasHost || hasReferer)
            {
                var hostname = hostName.FirstOrDefault();
                Serilog.Log.Information($"Getting host Host={hostName} Referer={referer}");
                if (hasHost)
                {
                    hostname = hostname.Contains(":") ? hostname.Split(":")[0] : hostname;
                }
                else
                {
                    hostname = referer.FirstOrDefault();
                    hostname = new Uri(referer).Host;
                }
                return await this._context.HostConfigs.AsSplitQuery().Where(host => host.Hostname == hostname).Include(host => host.HostLoginConfigs).Where(config => config.Deleted == null).ToListAsync();

            }
            else
            {
                return await this._context.HostConfigs.AsSplitQuery().Include(host => host.HostLoginConfigs).Where(config => config.Deleted == null).ToListAsync();
            }

        }


    }
}
