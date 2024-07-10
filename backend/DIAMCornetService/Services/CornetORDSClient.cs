namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using DIAMCornetService.Exceptions;
using DIAMCornetService.Models;
using DomainResults.Common;

public class CornetORDSClient(HttpClient httpClient, ILogger<CornetORDSClient> logger) : BaseClient(httpClient, logger), ICornetORDSClient
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    /// <exception cref="CornetException"></exception>
    public async Task<ParticipantResponseModel> GetCSNumberForParticipantAsync(string participantId)
    {

        var response = await this.GetAsync<CornetCSResponse>($"v1/participant/{participantId}/csnumber");


        if (response.IsSuccess)
        {
            return new ParticipantResponseModel()
            {
                CSNumber = response.Value.Csnum,
                ParticipantId = participantId
            };
        }
        else if (response.Status == DomainOperationStatus.NotFound)
        {
            return new ParticipantResponseModel()
            {
                ParticipantId = participantId,
                ErrorMessage = "Participant Not Found",
                ErrorType = CornetCSNumberErrorType.participantNotFound
            };
        }
        else
        {
            throw new CornetException($"Failed to get CS Number for participant {participantId}");
        }
    }

    /// <summary>
    /// Send a notification to eServices devices for a participant
    /// Can return a 200 OK but a responseCode that indicates a failure  
    /// </summary>
    /// <param name="model"></param>
    /// <param name="messageText"></param>
    /// <returns></returns>
    /// <exception cref="CornetException"></exception>
    public async Task<string> SubmitParticipantNotificationAsync(ParticipantResponseModel model, string messageText)
    {
        var response = await this.PostAsync<CornetResponse>($"v1/participant/{model.ParticipantId}/disclosure-message", new CornetMessageInput() { MessageTxt = messageText });

        var responseText = response.Value.ResponseCode;

        // todo determine possible response codes
        if (response.IsSuccess && string.IsNullOrEmpty(responseText))
        {

            logger.LogInformation($"Successfully submitted notification for participant {model.ParticipantId} CS: {model.CSNumber}");
            return "ok";

        }
        else if (response.IsSuccess && !string.IsNullOrEmpty(responseText))
        {
            logger.LogWarning($"Failed to submit notification for participant {model.ParticipantId} CS: {model.CSNumber} Reason: {responseText}");
            return responseText;
        }
        else
        {
            logger.LogError($"Failed to submit notification for participant {model.ParticipantId} CS: {model.CSNumber} Reason: {responseText}");

            throw new CornetException($"Failed to submit notification for participant {model.ParticipantId} CS: {model.CSNumber}");
        }
    }




    private class CornetCSResponse
    {
        public string Csnum { get; set; } = string.Empty;
    }

    public class CornetMessageInput
    {
        public string MessageTxt { get; set; } = string.Empty;
    }

    private class CornetResponse
    {
        public string ResponseCode { get; set; } = string.Empty;
    }
}

