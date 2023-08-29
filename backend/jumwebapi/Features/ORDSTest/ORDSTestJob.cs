namespace jumwebapi.Features.ORDSTest;

using System.Threading.Tasks;
using jumwebapi.Infrastructure.HttpClients.TestORDS;
using Prometheus;
using Quartz;

public class ORDSTestJob : IJob
{

    private static readonly Histogram ORDSTestDuration = Metrics
         .CreateHistogram("jum_ords_test_duration", "Histogram of JUM ORDS TEST job executions");

    private readonly ITestORDSClient ordsClient;
    public ORDSTestJob(ITestORDSClient ordsClient) => this.ordsClient = ordsClient;


    public async Task Execute(IJobExecutionContext context)
    {

        using (ORDSTestDuration.NewTimer())
        {

            Serilog.Log.Information($"Running ORDS test");
            var result = await this.ordsClient.GetRandomCaseInfo();
        }
    }
}
