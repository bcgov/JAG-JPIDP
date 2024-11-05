namespace edt.casemanagement.HttpClients.Services.EdtCore;

using System.Collections.Generic;
using Common.Models.EDT;
using edt.casemanagement.Features.Cases;
using edt.casemanagement.Models;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;

public interface IEdtClient
{
    Task<Task> HandleCaseRequest(SubAgencyDomainEvent accessRequest);

    Task<IEnumerable<CustomFieldDefinition>> GetCustomFields(string objectType);

    Task<EdtUserDto?> GetUser(string userKey);

    /// <summary>
    /// Get the version of EDT (also acts as a simple ping test)
    /// </summary>
    /// <returns></returns>
    Task<string> GetVersion();


    /// <summary>
    /// Get a case based on the KEY (case Number)
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<CaseModel> FindCase(CaseLookupQuery query);

    /// <summary>
    /// Get a case based on the case Id
    /// </summary>
    /// <param name="caseId"></param>
    /// <returns></returns>
    Task<CaseModel> GetCase(int caseId);

    /// <summary>
    /// Gets summary information only
    /// This is not affected by the state of the case (unlike GetCase)
    /// It is primarily used to detemine if the case is currently being processed
    /// </summary>
    /// <param name="caseId"></param>
    /// <returns></returns>
    Task<CaseSummaryModel> GetCaseSummary(int caseId);

    /// <summary>
    /// Get the case Ids currently assigned to a user
    /// </summary>
    /// <param name="userKey"></param>
    /// <returns></returns>
    Task<IEnumerable<KeyIdPair>> GetUserCases(string userKey);

    /// <summary>
    /// Add the user to the case Id
    /// </summary>
    /// <param name="userKey"></param>
    /// <param name="caseId"></param>
    /// <returns></returns>
    Task<bool> AddUserToCase(string userKey, int caseId);


    /// <summary>
    /// Get the case groups assigned to the user/case combination
    /// </summary>
    /// <param name="userKey"></param>
    /// <param name="caseId"></param>
    /// <returns></returns>
    Task<IEnumerable<UserCaseGroup>> GetUserCaseGroups(string userKey, int caseId);


    /// <summary>
    /// Add the user thats in the case to the given group (e.g. Submitting Agency)
    /// </summary>
    /// <param name="userKey"></param>
    /// <param name="caseId"></param>
    /// <param name="caseGroupId"></param>
    /// <returns></returns>
    Task<bool> AddUserToCaseGroup(string userKey, int caseId, int caseGroupId);



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
