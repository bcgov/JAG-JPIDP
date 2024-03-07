namespace jumwebapi.Infrastructure.HttpClients.JustinCases;

using global::Common.Models.JUSTIN;

public interface IJustinCaseClient
{
    Task<CaseStatus> GetCaseStatus(string caseId, string accessToken);
}
