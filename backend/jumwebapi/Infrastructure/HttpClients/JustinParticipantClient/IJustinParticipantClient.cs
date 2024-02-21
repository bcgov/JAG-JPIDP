namespace jumwebapi.Infrastructure.HttpClients.JustinParticipant;

using global::Common.Models.JUSTIN;

public interface IJustinParticipantClient
{
    Task<Participant> GetParticipantByUserName(string username, string accesToken);
    Task<Participant> GetParticipantPartId(decimal partId, string accesToken);
}
