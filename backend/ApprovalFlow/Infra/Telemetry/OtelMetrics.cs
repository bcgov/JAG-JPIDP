namespace ApprovalFlow.Telemetry;

using System.Diagnostics.Metrics;

public class OtelMetrics
{
    private Counter<int> IncomingApprovalCounter { get; }



    public string MetricName { get; }

    public OtelMetrics(string meterName = "ApprovalFlow")
    {
        var meter = new Meter(meterName);
        MetricName = meterName;
        IncomingApprovalCounter = meter.CreateCounter<int>("incoming-approvals", "Approvals");

    }

    public void AddApproval() => IncomingApprovalCounter.Add(1);


}
