namespace Pidp.Infrastructure.HttpClients.Jum;

using Common.Models.JUSTIN;
using CommonModels.Models.JUSTIN;
using Pidp.Models;

public interface IJumClient
{
    Task<Participant?> GetJumUserAsync(string username);
    Task<Participant?> GetJumUserAsync(string username, string accessToken);
    Task<Participant?> GetJumUserByPartIdAsync(decimal partId);
    Task<Participant?> GetJumUserByPartIdAsync(string partId);
    Task<Participant?> GetParticipantByUserNameAsync(string username);


    Task<Participant?> GetJumUserByPartIdAsync(decimal partId, string accessToken);
    Task<bool> IsJumUser(Participant? justinUser, Party party);

    Task<CaseStatusWrapper> GetJustinCaseStatus(string partyId, string caseId, string accessToken);

    Task<bool> FlagUserUpdateAsComplete(int messageId, bool isSuccessful);
}
