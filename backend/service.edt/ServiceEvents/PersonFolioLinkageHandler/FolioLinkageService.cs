namespace edt.service.ServiceEvents.PersonFolioLinkageHandler;

using edt.service.Data;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.ServiceEvents.PersonCreationHandler.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;

/// <summary>
/// This class will process the actual linkage between particpants and folios over in disclosure
/// It will also clean up any completed requests
/// </summary>
[DisallowConcurrentExecution]
public class FolioLinkageService : IFolioLinkageService
{

    private readonly EdtServiceConfiguration config;
    private readonly ILogger<FolioLinkageJob> logger;
    private readonly IEdtClient edtClient;
    private readonly DbContextOptions<EdtDataStoreDbContext> dbOptions;
    private readonly IClock clock;


    public FolioLinkageService(
        EdtServiceConfiguration config,
        IEdtClient edtClient, DbContextOptions<EdtDataStoreDbContext> dbOptions,
        ILogger<FolioLinkageJob> logger,
        IClock clock)
    {
        this.config = config;
        this.logger = logger;
        this.edtClient = edtClient;
        this.dbOptions = dbOptions;
        this.clock = clock;
    }

    public async Task<int> ProcessPendingRequests()
    {

        using (var db = new EdtDataStoreDbContext(this.dbOptions, this.clock, this.config))
        {

            Serilog.Log.Debug($"Before Changes {db.ChangeTracker.DebugView.LongView}");

            var pending = db.FolioLinkageRequests.Where(req => req.Status == "Pending").ToList();
            if (pending.Count > 0)
            {
                Serilog.Log.Information($"Found {pending.Count} pending folio linkage requests");
            }
            var tasks = pending.Select(p => this.LinkFolio(p));
            await Task.WhenAll(tasks);

            Serilog.Log.Debug($"After Changes {db.ChangeTracker.DebugView.LongView}");

            return await db.SaveChangesAsync();
        }
    }


    public async Task<PersonFolioLinkage> LinkFolio(PersonFolioLinkage request)
    {
        this.logger.LogPendingRequestItem(request.PersonKey, request.DisclosureCaseIdentifier);
        var complete = await this.edtClient.LinkPersonToDisclosureFolio(request);
        if (complete)
        {
            Serilog.Log.Information($"Marking folio link request complete {request.PersonKey} {request.DisclosureCaseIdentifier}");
            // mark the request as done
            request.Status = "Complete";
            request.Modified = this.clock.GetCurrentInstant();

        }
        else
        {
            Serilog.Log.Information($"Folio request incomplete - {request.RetryCount} {request.PersonKey} {request.DisclosureCaseIdentifier}");

            request.Modified = this.clock.GetCurrentInstant();
            request.RetryCount++;

            if (request.RetryCount > this.config.FolioLinkageBackgroundService.MaxRetriesForLinking)
            {
                this.logger.LogMaxRetriesExceeded(request.PersonKey, request.DisclosureCaseIdentifier);
                request.Status = "Max Retries";
            }

        }

        return request;
    }
}
public static partial class FolioLinkageServiceLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Folio linkage service processing pending {count} requests")]
    public static partial void LogProcessingPending(this ILogger logger, int count);
    [LoggerMessage(2, LogLevel.Information, "Processing folio link {key} {discId}")]
    public static partial void LogPendingRequestItem(this ILogger logger, string key, string discId);
    [LoggerMessage(3, LogLevel.Error, "Max retries for folio link {key} {discId} - check particpant info")]
    public static partial void LogMaxRetriesExceeded(this ILogger logger, string key, string discId);
}
