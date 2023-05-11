namespace edt.disclosure.Telemetry;

using System.Diagnostics;
using edt.disclosure.Infrastructure.Telemetry;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

}
