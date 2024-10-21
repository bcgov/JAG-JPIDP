namespace jumwebapi.Infrastructure.HttpClients.JustinCases;

using CommonModels.Models.JUSTIN;

public interface IJustinCaseClient
{
    Task<CaseStatusWrapper> GetCaseStatus(string caseId, string accessToken);
}
