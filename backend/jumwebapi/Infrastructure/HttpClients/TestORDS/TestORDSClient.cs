namespace jumwebapi.Infrastructure.HttpClients.TestORDS;

using System;
using System.Threading.Tasks;

public class TestORDSClient : BaseClient, ITestORDSClient
{

    public TestORDSClient(HttpClient httpClient, ILogger<TestORDSClient> logger) : base(httpClient, logger) { }

    public async Task<string> GetRandomCaseInfo()
    {

        // get random case betwee 40000 and 40084
        var random = new Random();
        var randomNumber = random.Next(40000, 40084);

        var response = await this.GetAsync<string>($"courtFile?mdoc_justin_no={randomNumber}");

        Serilog.Log.Information($"Response {response}");
        return "test";
    }
}
