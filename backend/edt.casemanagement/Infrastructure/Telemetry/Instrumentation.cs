namespace edt.casemanagement.Infrastructure.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "EDT.CaseManagement";
    internal const string MeterName = "EDT.CaseManagement";

    private readonly Meter meter;




    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();

        this.meter = new Meter(MeterName, version);
        this.ActivitySource = new ActivitySource(ActivitySourceName, version);
        this.CaseSearchCount = this.meter.CreateCounter<long>("case_search_count_total", description: "Number of case searches");
        this.CaseStatusDuration = this.meter.CreateHistogram<double>("case_status_lookup_duration", description: "Case access duration", unit: "ms");
        this.ProcessedJobCount = this.meter.CreateCounter<long>("case_access_count_total", description: "Number of total case requests");
        this.ProcessRemovedJob = this.meter.CreateCounter<long>("case_removal_count_total", description: "Number of total case removal requests");

    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> CaseSearchCount { get; }
    public Histogram<double> CaseStatusDuration { get; }
    public Counter<long> ProcessedJobCount { get; }
    public Counter<long> ProcessRemovedJob { get; }


    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.meter.Dispose();
    }
}
