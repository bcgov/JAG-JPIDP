namespace edt.service.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;

public class OtelMetrics
{
    private Counter<int> EdtAddUserCounter { get; }
    private Counter<int> EdtAddPersonCounter { get; }
    private Counter<int> EdtUpdatePersonCounter { get; }

    
    private Counter<int> EdtUpdateUserCounter { get; }
    private Counter<int> EdtGetUserCounter { get; }
    private Counter<int> EdtGetPersonCounter { get; }

    public string MetricName { get; }

    public OtelMetrics(string meterName = "EDTMeter")
    {
        var meter = new Meter(meterName);
        MetricName = meterName;
        EdtGetUserCounter = meter.CreateCounter<int>("edt-user-get", "EDTUser");
        EdtGetPersonCounter = meter.CreateCounter<int>("edt-person-get", "EDTUser");
        EdtUpdatePersonCounter = meter.CreateCounter<int>("edt-person-update", "EDTUser");

        EdtAddUserCounter = meter.CreateCounter<int>("edt-user-added", "EDTUser");
        EdtAddPersonCounter = meter.CreateCounter<int>("edt-person-added", "EDTUser");
        EdtUpdateUserCounter = meter.CreateCounter<int>("edt-user-update", "EDTUser");
    }

    public void AddUser() => EdtAddUserCounter.Add(1);
    public void UpdateUser() => EdtUpdateUserCounter.Add(1);
    public void AddPerson() => EdtAddPersonCounter.Add(1);
    public void GetPerson() => EdtGetPersonCounter.Add(1);
    public void UpdatePerson() => EdtAddPersonCounter.Add(1);

    public void GetUser() => EdtGetUserCounter.Add(1);

}
