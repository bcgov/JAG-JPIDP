using jumwebapi.Features.Participants.Models;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;

namespace jumwebapi.Features.Participants.Services.ParticipantService;
public class ParticipantService : IParticipantService
{
    public Task<Participant> GetParticipantByUserName(string username)
    {
        throw new NotImplementedException();
    }

    public Task<Participant> GetParticipantPartId(long partId)
    {
        throw new NotImplementedException();
    }
}
