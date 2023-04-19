namespace jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using jumwebapi.Features.Participants.Models;

public interface IJustinParticipantClient
{
    Task<Participant> GetParticipantByUserName(string username, string accesToken);
    Task<Participant> GetParticipantPartId(decimal partId, string accesToken);
}
