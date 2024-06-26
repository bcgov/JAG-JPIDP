namespace ISLInterfaces.Infrastructure.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "ISL.CaseQuery";
    internal const string MeterName = "ISL.CaseQuery";

    private readonly Meter meter;




    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();

        this.meter = new Meter(MeterName, version);
        this.ActivitySource = new ActivitySource(ActivitySourceName, version);
        this.CaseSearchCount = this.meter.CreateCounter<long>("diam_case_search_count_total", description: "Number of case searches");
        this.CaseActiveUsersCount = this.meter.CreateCounter<long>("diam_case_search_active_users_count_total", description: "Number of case search users returned");

        this.CaseStatusDuration = this.meter.CreateHistogram<double>("diam_case_status_lookup_duration", description: "Case access duration", unit: "ms");


    }

    public ActivitySource ActivitySource { get; }
    public Counter<long> CaseSearchCount { get; }

    public Counter<long> CaseActiveUsersCount { get; }
    public Histogram<double> CaseStatusDuration { get; }


    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.meter.Dispose();
    }
}
