namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

using System.Collections.Generic;
using edt.disclosure.ServiceEvents.CourtLocation.Models;

public interface IEdtDisclosureClient
{
 //   Task<Task> CreateDisclosureUser(string key, CourtLocationDomainEvent accessRequest);


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
}
