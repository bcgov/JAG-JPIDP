namespace DIAMCornetService.Services;
using DIAMCornetService.Models;

public interface ICornetService
{
    /// <summary>
    /// Get the CS number for a participant ID
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public Task<InCustodyParticipantModel> LookupCSNumberForParticipant(string participantId);

    /// <summary>
    /// Submit a message to eServices devices for Participant
    /// </summary>
    /// <param name="participantId"></param>
    /// <param name="csNumber"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task<InCustodyParticipantModel> SubmitNotificationToEServices(InCustodyParticipantModel model, string message);

    /// <summary>
    /// Publish the participant and CS Number to the DIAM topic
    /// </summary>
    /// <param name="participantId"></param>
    /// <param name="messageText"></param>
    /// <returns></returns>
    public Task<InCustodyParticipantModel> PublishNotificationToDIAMAsync(InCustodyParticipantModel model);

    /// <summary>
    /// Publish and Error notifications to DIAM
    /// </summary>
    /// <param name="participantId"></param>
    /// <param name="messageText"></param>
    /// <returns></returns>
    public Task<InCustodyParticipantModel> PublishErrorsToDIAMAsync(InCustodyParticipantModel model);
}
