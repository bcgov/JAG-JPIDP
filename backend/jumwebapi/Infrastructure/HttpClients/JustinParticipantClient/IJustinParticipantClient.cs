namespace jumwebapi.Infrastructure.HttpClients.JustinParticipant;

using global::Common.Models.JUSTIN;

public interface IJustinParticipantClient
{
    Task<Participant> GetParticipantByUserName(string username, string accesToken);
    Task<Participant> GetParticipantByPartId(string partId, string accesToken);
    Task<Participant> GetParticipantByPartId(decimal partId, string accesToken);
}
