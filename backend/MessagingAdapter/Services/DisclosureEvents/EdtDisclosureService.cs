namespace MessagingAdapter.Services.DisclosureEvents;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessagingAdapter.Data;
using MessagingAdapter.Models;

/// <summary>
/// Get Disclosure events from EDT Core
/// </summary>
public class EdtDisclosureService : BackgroundService, IEdtDisclosureService
{

    private IEdtCoreClient edtCoreClient;
    private IConfiguration configuration;
    private MessagingAdapterContext dbContext;
    private ILogger<EdtDisclosureService> logger;

    public EdtDisclosureService(IEdtCoreClient edtCoreClient, IConfiguration configuration, MessagingAdapterContext dbContext, ILogger<EdtDisclosureService> logger)
    {
        this.edtCoreClient = edtCoreClient;
        this.configuration = configuration;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<List<DisclosureEventModel>> GetDisclosureEventsAsync()
    {



        return null;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {

    }
}


