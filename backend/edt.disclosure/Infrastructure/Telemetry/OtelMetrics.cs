namespace edt.disclosure.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;

public class OtelMetrics
{
    private Counter<int> EdtAddUserCounter { get; }
    private Counter<int> EdtUpdateUserCounter { get; }
    private Counter<int> EdtGetUserCounter { get; }

    public string MetricName { get; }

    public OtelMetrics(string meterName = "EDTMeter")
    {
        var meter = new Meter(meterName);
        MetricName = meterName;
        EdtGetUserCounter = meter.CreateCounter<int>("edt-user-get", "EDTUser");
        EdtAddUserCounter = meter.CreateCounter<int>("edt-user-added", "EDTUser");
        EdtUpdateUserCounter = meter.CreateCounter<int>("edt-user-update", "EDTUser");
    }

    public void AddUser() => EdtAddUserCounter.Add(1);
    public void UpdateUser() => EdtUpdateUserCounter.Add(1);
    public void GetUser() => EdtGetUserCounter.Add(1);

}
