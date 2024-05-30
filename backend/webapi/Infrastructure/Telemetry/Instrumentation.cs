namespace Pidp.Infrastructure.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "DIAM.WebAPI";
    internal const string MeterName = "DIAM.WebAPI";

    private readonly Meter meter;




    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();

        this.meter = new Meter(MeterName, version);
        this.ActivitySource = new ActivitySource(ActivitySourceName, version);

        this.OnBoardingRequests = this.meter.CreateCounter<long>("diam-user-onboarding", description: "DIAM onboarding requests");
        this.OnBoardingRequestTime = this.meter.CreateHistogram<double>("diam-user-onboarding-duration", description: "DIAM onboarding requests", unit: "ms");

    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> OnBoardingRequests { get; }
    public Histogram<double> OnBoardingRequestTime { get; }




    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.meter.Dispose();
    }
}
