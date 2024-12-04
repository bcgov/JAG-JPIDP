namespace edt.service.Features.Participant;

using CommonModels.Models.Party;

public interface IParticipantLookupService
{
    /// <summary>
    /// Get details on all participants related to the given participantID - whether this is the primary or a merged participant
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public Task<ParticipantMergeListingModel> GetParticipantMergeDetails(string participantId);
}
