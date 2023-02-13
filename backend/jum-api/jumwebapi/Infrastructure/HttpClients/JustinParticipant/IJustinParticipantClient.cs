using jumwebapi.Features.Participants.Models;

namespace jumwebapi.Infrastructure.HttpClients.JustinParticipant
{
    public interface IJustinParticipantClient
    {
        Task<Participant> GetParticipantByUserName(string username, string accesToken);
        Task<Participant> GetParticipantPartId(decimal partId, string accesToken);
    }
}
