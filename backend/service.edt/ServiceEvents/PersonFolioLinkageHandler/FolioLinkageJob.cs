namespace edt.service.ServiceEvents.PersonFolioLinkageHandler;

using System.Threading.Tasks;
using Quartz;

public class FolioLinkageJob : IJob
{
    private readonly IFolioLinkageService service;
    private readonly ILogger logger;



    public FolioLinkageJob(
        IFolioLinkageService service,
        ILogger<FolioLinkageJob> logger)
    {
        this.logger = logger;
        this.service = service;
    }


    public Task Execute(IJobExecutionContext context)
    {
        this.logger.LogStarting();
        try
        {
            this.service.ProcessPendingRequests();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Folio Linkage Error {ex.Message}");
            return Task.FromException(ex);
        }
        finally
        {
            this.logger.LogStopping();
        }
    }
}

public static partial class FolioLinkageBackgroundServiceLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "FolioLinkageJob is starting")]
    public static partial void LogStarting(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "FolioLinkageJob is stopping")]
    public static partial void LogStopping(this ILogger logger);

    [LoggerMessage(3, LogLevel.Information, "FolioLinkageBackgroundService processing...")]
    public static partial void LogProcessing(this ILogger logger);

    [LoggerMessage(4, LogLevel.Information, "Processing linkage {folioId} for key {key}")]
    public static partial void LogProcessingRequest(this ILogger logger, string key, string folioId);
}
