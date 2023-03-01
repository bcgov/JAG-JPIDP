namespace edt.casemanagement.HttpClients.Services.EdtCore;

using edt.casemanagement.Features.Cases;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;

public interface IEdtClient
{
    Task<Task> HandleCaseRequest(string key, SubAgencyDomainEvent accessRequest);


    Task<EdtUserDto?> GetUser(string userKey);

    /// <summary>
    /// Get the version of EDT (also acts as a simple ping test)
    /// </summary>
    /// <returns></returns>
    Task<string> GetVersion();


    /// <summary>
    /// Get a case based on the KEY (case Number)
    /// </summary>
    /// <param name="caseNumber"></param>
    /// <returns></returns>
    Task<CaseModel> FindCase(string caseNumber);

}
