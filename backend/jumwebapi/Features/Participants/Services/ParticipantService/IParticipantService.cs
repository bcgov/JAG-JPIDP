using Common.Models.JUSTIN;

namespace jumwebapi.Features.Participants.Services.ParticipantService
{
    public interface IParticipantService
    {
        Task<Participant> GetParticipantByUserName(string username);
        Task<Participant> GetParticipantPartId(long partId);
    }
}
