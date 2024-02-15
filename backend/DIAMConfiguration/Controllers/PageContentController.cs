namespace DIAMConfiguration.Controllers;

using Common.Utils;
using DIAMConfiguration.Data;
using DIAMConfiguration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/page-content/{pageId}")]
[ApiController]
public class PageContentController : ControllerBase
{
    private readonly DIAMConfigurationDataStoreDbContext context;
    public PageContentController(DIAMConfigurationDataStoreDbContext context)
    {
        this.context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PageContentModel>> GetPageContents(string pageId)
    {

        var hostname = HttpUtils.GetHostFromHeader(this.HttpContext.Request);
        if (hostname == null)
        {
            return this.NotFound();
        }
        var hostConfig = this.context.HostConfigs.Where(host => host.Hostname == hostname).FirstOrDefault();

        if (hostConfig == null)
        {
            return this.NotFound();
        }

        // get contents for host/pageId combo
        var contents = this.context.PageContents.Where(content => content.Hosts.Contains(hostConfig) && content.ContentKey == pageId).ToListAsync();

        return this.Ok(Array.Empty<PageContentModel>());

    }
}
