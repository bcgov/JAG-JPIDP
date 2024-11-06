namespace edt.service.Infrastructure.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "EDT.Core";
    internal const string MeterName = "EDT.Core";

    private readonly Meter meter;




    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();

        this.meter = new Meter(MeterName, version);
        this.ActivitySource = new ActivitySource(ActivitySourceName, version);

        this.EdtGetUserCounter = this.meter.CreateCounter<long>("edt-user-get", description: "EDT User Get Requests");
        this.EdtAddUserCounter = this.meter.CreateCounter<long>("edt-user-added", description: "EDT User Add Requests");
        this.EdtUpdateUserCounter = this.meter.CreateCounter<long>("edt-user-update", description: "EDT User Update Requests");
        this.EdtUpdatePersonCounter = this.meter.CreateCounter<long>("edt-person-update", description: "EDT person Update Requests");
        this.EdtGetPersonCounter = this.meter.CreateCounter<long>("edt-person-get", description: "EDT Person Get Requests");
        this.EdtAddPersonCounter = this.meter.CreateCounter<long>("edt-person-added", description: "EDT Person Add Requests");
        this.ParticipantMergeCounter = this.meter.CreateCounter<long>("edt-participantmergeinfo-request", description: "EDT Participant Merge Lookup Requests");


    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> EdtGetUserCounter { get; }
    public Counter<long> EdtGetPersonCounter { get; }
    public Counter<long> EdtUpdatePersonCounter { get; }
    public Counter<long> EdtAddUserCounter { get; }
    public Counter<long> EdtAddPersonCounter { get; }
    public Counter<long> EdtUpdateUserCounter { get; }
    public Counter<long> ParticipantMergeCounter { get; }



    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.meter.Dispose();
    }
}
