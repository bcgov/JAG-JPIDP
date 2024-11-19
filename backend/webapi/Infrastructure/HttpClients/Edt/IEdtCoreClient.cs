namespace Pidp.Infrastructure.HttpClients.Edt;

using Common.Models.EDT;
using CommonModels.Models.Party;

public interface IEdtCoreClient
{
    Task<List<EdtPersonDto>?> GetPersonsByIdentifier(string identitiferType, string identifierValue);
    Task<EdtPersonDto>? GetPersonByKey(string key);
    Task<List<EdtPersonDto>?> FindPersons(PersonLookupModel personLookupModel);
    Task<EdtUserDto?> GetUserByKey(string key);
    Task<List<UserCaseSearchResponseModel>?> GetUserCases(string userId);
    Task<ParticipantMergeListingModel> GetParticipantMergeListing(string participantId);

}
