namespace edt.service.ServiceEvents.PersonFolioLinkageHandler;

using edt.service.Data;
using edt.service.HttpClients.Services.EdtCore;
using NodaTime;

/// <summary>
/// This class will process the actual linkage between particpants and folios over in disclosure
/// It will also clean up any completed requests
/// </summary>
public class FolioLinkageService : IFolioLinkageService
{

    private readonly EdtServiceConfiguration config;
    private readonly ILogger<FolioLinkageJob> logger;
    private readonly IEdtClient edtClient;
    private readonly EdtDataStoreDbContext context;
    private readonly IClock clock;


    public FolioLinkageService(
        EdtServiceConfiguration config,
        EdtDataStoreDbContext context,
        IEdtClient edtClient,
        ILogger<FolioLinkageJob> logger,
        IClock clock)
    {
        this.config = config;
        this.logger = logger;
        this.context = context;
        this.edtClient = edtClient;
        this.clock = clock;
    }

    public async Task<int> ProcessPendingRequests()
    {
        var processedCount = 0;
        var pending = this.context.FolioLinkageRequests.Where(req => req.Status == "Pending").ToList();
        this.logger.LogProcessingPending(pending.Count);
        foreach (var request in pending)
        {
            this.context.FolioLinkageRequests.Attach(request);

            this.logger.LogPendingRequestItem(request.PersonKey, request.DisclosureCaseIdentifier);
            var complete = await this.edtClient.LinkPersonToDisclosureFolio(request);
            if (complete)
            {
                // mark the request as done
                request.Status = "Complete";
                request.Modified = this.clock.GetCurrentInstant();
                processedCount++;
            }
            else
            {
                request.Modified = this.clock.GetCurrentInstant();
                request.RetryCount++;

                if (request.RetryCount > this.config.FolioLinkageBackgroundService.MaxRetriesForLinking)
                {
                    this.logger.LogMaxRetriesExceeded(request.PersonKey, request.DisclosureCaseIdentifier);
                    request.Status = "Max Retries";
                }
            }

            await this.context.SaveChangesAsync();
        }

        return processedCount;
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
