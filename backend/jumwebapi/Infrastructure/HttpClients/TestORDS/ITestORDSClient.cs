namespace jumwebapi.Infrastructure.HttpClients.TestORDS;

public interface ITestORDSClient
{
    public Task<string> GetRandomCaseInfo();
}
