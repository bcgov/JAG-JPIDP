namespace edt.casemanagement.Telemetry;

using System.Diagnostics;
using edt.casemanagement.Infrastructure.Telemetry;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

}
