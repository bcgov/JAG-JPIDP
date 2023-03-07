namespace edt.casemanagement.HttpClients.Services.EdtCore;

using System.Collections.Generic;
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

    /// <summary>
    /// Get the case Ids currently assigned to a user
    /// </summary>
    /// <param name="userKey"></param>
    /// <returns></returns>
    Task<IEnumerable<int>> GetUserCases(string userKey);

    /// <summary>
    /// Add the user to the case Id
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="caseId"></param>
    /// <returns></returns>
    Task<bool> AddUserToCase(string userId, int caseId);


    /// <summary>
    /// Add the user thats in the case to the given group (e.g. Submitting Agency)
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="caseId"></param>
    /// <param name="caseGroupId"></param>
    /// <returns></returns>
    Task<bool> AddUserToCaseGroup(string userId, int caseId, int caseGroupId);



    /// <summary>
    /// For a given case get the ID of the group name provided
    /// </summary>
    /// <param name="caseId"></param>
    /// <param name="groupName"></param>
    /// <returns></returns>
    Task<int> GetCaseGroupId(int caseId, string groupName);

    /// <summary>
    /// Remove the user from the case
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="caseId"></param>
    /// <returns></returns>
    Task<bool> RemoveUserFromCase(string userId, int caseId);
}
