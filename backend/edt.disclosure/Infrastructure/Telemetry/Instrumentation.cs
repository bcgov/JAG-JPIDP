namespace edt.disclosure.Infrastructure.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "EDT.Disclosure";
    internal const string MeterName = "EDT.Disclosure";

    private readonly Meter meter;

    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();

        this.meter = new Meter(MeterName, version);
        this.ActivitySource = new ActivitySource(ActivitySourceName, version);
        this.EdtGetUserCounter = this.meter.CreateCounter<long>("edt-user-get", description: "EDT User Get Requests");

        this.EdtAddUserCounter = this.meter.CreateCounter<long>("edt-user-added", description: "EDT User Add Requests");
        this.EdtUpdateUserCounter = this.meter.CreateCounter<long>("edt-user-update", description: "EDT User Update Requests");
    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> EdtGetUserCounter { get; }
    public Counter<long> EdtAddUserCounter { get; }
    public Counter<long> EdtUpdateUserCounter { get; }

    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.meter.Dispose();
    }
}
