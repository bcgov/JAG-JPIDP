namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

using System.Collections.Generic;
using edt.disclosure.Features.Cases;
using edt.disclosure.Kafka.Model;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.UserAccountModification.Models;

public interface IEdtDisclosureClient
{
    //   Task<Task> CreateDisclosureUser(string key, CourtLocationDomainEvent accessRequest);

    Task<UserModificationEvent> CreateUser(EdtDisclosureUserProvisioningModel accessRequest);
    Task<UserModificationEvent> UpdateUser(EdtDisclosureUserProvisioningModel accessRequest, EdtUserDto currentUser);
    Task<UserModificationEvent> UpdateUser(UserChangeModel changeEvent);

    Task<CaseModel> FindCase(string field, string value);
    Task<CaseModel> GetCase(int caseID);
    Task<CaseModel> GetCase(int caseID, bool includeFields);

    Task<CaseModel> FindCaseByKey(string caseKey);
    Task<CaseModel> FindCaseByKey(string caseKey, bool includeFields);
    Task<CaseModel> FindCaseByIdentifier(string identifierType, string identifierValue);

    Task<EdtUserDto?> GetUser(string userKey);

    /// <summary>
    /// Get the version of EDT (also acts as a simple ping test)
    /// </summary>
    /// <returns></returns>
    Task<string> GetVersion();

    Task<Task> HandleCourtLocationRequest(string key, CourtLocationDomainEvent accessRequest);
    /// <summary>
    /// Get a case based on the KEY (case Number)
    /// </summary>
    /// <param name="caseNumber"></param>
    /// <returns></returns>
    Task<CourtLocationCaseModel> FindLocationCase(string caseNumber);
    /// <summary>
    /// Create a new case
    /// </summary>
    /// <param name="caseCreation"></param>
    /// <returns>Id of the newly created case</returns>
    Task<int> CreateCase(EdtCaseDto caseCreation);
    Task<IEnumerable<KeyIdPair>> GetUserCases(string? userID);
    Task<bool> AddUserToCase(string? userID, int caseID);
    Task<bool> AddUserToCase(string? userID, int caseID, string caseGroupName);
    Task<bool> AddUserToCase(string? userID, int caseID, int caseGroupID);

    Task<bool> AddUserToCaseGroup(string? userID, int caseID, int caseGroupID);
}
