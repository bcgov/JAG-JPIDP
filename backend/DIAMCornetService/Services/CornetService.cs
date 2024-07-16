namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using Common.Kafka;
using Confluent.Kafka;
using DIAM.Common.Models;
using DIAMCornetService.Exceptions;
using DIAMCornetService.Models;
using NodaTime;

public class CornetService(ILogger<CornetService> logger, IKafkaProducer<string, InCustodyParticipantModel> producer, IKafkaProducer<string, GenericProcessStatusResponse> errorProducer, DIAMCornetServiceConfiguration cornetServiceConfiguration, ICornetORDSClient cornetORDSClient) : ICornetService
{

    /// <summary>
    /// Publish message to DIAM topic
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="DIAMKafkaException"></exception>
    public async Task<InCustodyParticipantModel> PublishNotificationToDIAMAsync(InCustodyParticipantModel model)
    {
        try
        {
            var guid = Guid.NewGuid();

            model.CreationDateUTC = DateTime.UtcNow;

            var response = await producer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.ParticipantCSNumberMappingTopic, guid.ToString(), model);

            if (response.Status != PersistenceStatus.Persisted)
            {
                logger.DIAMResponseFailed(model.ParticipantId, cornetServiceConfiguration.KafkaCluster.ParticipantCSNumberMappingTopic, response.Status.ToString());
                throw new DIAMKafkaException($"Failed to publish cs number mapping for {guid.ToString()} Part: {model.ParticipantId} CSNumber: {model.CSNumber}");
            }
            else
            {
                model.DIAMPublishId = guid.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.DIAMResponseFailed(model.ParticipantId, cornetServiceConfiguration.KafkaCluster.ParticipantCSNumberMappingTopic, ex.Message);
            throw;
        }

        return model;
    }


    public async Task<InCustodyParticipantModel> PublishErrorsToDIAMAsync(InCustodyParticipantModel model)
    {
        try
        {
            var guid = Guid.NewGuid();

            var responseModel = new GenericProcessStatusResponse()
            {
                DomainEvent = "digitalevidence-incustody-notify-failure",
                ErrorList = [model.ErrorMessage],
                EventTime = SystemClock.Instance.GetCurrentInstant(),
                PartId = model.ParticipantId!,
                Status = "Error"
            };

            model.CreationDateUTC = DateTime.UtcNow;

            var response = await errorProducer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic, guid.ToString(), responseModel);

            if (response.Status != PersistenceStatus.Persisted)
            {
                logger.DIAMErrorResponseFailed(model.ParticipantId, cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic, response.Status.ToString());

                throw new DIAMKafkaException($"Failed to publish Error response for {model.ParticipantId} to {cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic}");
            }
            else
            {
                model.DIAMPublishId = guid.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.DIAMErrorResponseFailed(model.ParticipantId, cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic, ex.Message);
            throw;
        }

        return model;

    }

    /// <summary>
    /// Simple CS Number lookup
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public async Task<InCustodyParticipantModel> LookupCSNumberForParticipant(string participantId)
    {
        var responseModel = await cornetORDSClient.GetCSNumberForParticipantAsync(participantId);

        return responseModel;

    }


    /// <summary>
    /// Publish a notification within the CORNET system
    /// </summary>
    /// <param name="csNumber"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<InCustodyParticipantModel> SubmitNotificationToEServices(InCustodyParticipantModel model, string message)
    {

        logger.PublishingToEServices(model.ParticipantId, model.CSNumber);

        var response = await cornetORDSClient.SubmitParticipantNotificationAsync(model, message);

        switch (response)
        {
            case "SUCC":
            {
                logger.NotificationSubmissionSuccessful(model.ParticipantId, model.CSNumber);
                break;
            }
            case "NABI":
            {
                logger.NotificationSubmissionFailed(model.ParticipantId, model.CSNumber, response);
                model.ErrorMessage = "Participant has no active BioMetrics";
                model.ErrorType = CornetCSNumberErrorType.noActiveBioMetrics;
                break;
            }
            case "ENDP":
            {
                logger.NotificationSubmissionFailed(model.ParticipantId, model.CSNumber, response);
                model.ErrorMessage = "eDisclosure not provisioned for user";
                model.ErrorType = CornetCSNumberErrorType.eDisclosureNotProvisioned;
                break;
            }
            // shouldnt get to this one as CS number is looked up previously but added to cover all responses
            case "MISC":
            {
                logger.NotificationSubmissionFailed(model.ParticipantId, model.CSNumber, response);
                model.ErrorMessage = "Missing CS Number";
                model.ErrorType = CornetCSNumberErrorType.missingCSNumber;
                break;
            }
            case "OTHR":
            {
                logger.NotificationSubmissionFailed(model.ParticipantId, model.CSNumber, response);
                model.ErrorMessage = "Unknown CORNET error occurred";
                model.ErrorType = CornetCSNumberErrorType.otherError;
                break;
            }
            default:
            {
                logger.NotificationSubmissionFailed(model.ParticipantId, model.CSNumber, response);
                model.ErrorMessage = $"Unknown CORNET response {response}";
                model.ErrorType = CornetCSNumberErrorType.unknownResponseError;
                break;
            }
        }

        return model;

    }

}

public static partial class CornetServiceLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Successfully published notification for partId: {participantId} CS: {CSNumber}")]
    public static partial void NotificationSubmissionSuccessful(this ILogger logger, string? participantId, string? CSNumber);
    [LoggerMessage(2, LogLevel.Error, "Failed to submit eServices notification for partId: {participantId} CS: {CSNumber} - Reason {reason}")]
    public static partial void NotificationSubmissionFailed(this ILogger logger, string? participantId, string? CSNumber, string reason);
    [LoggerMessage(3, LogLevel.Error, "Failed to publish Error response for {participantId} to {topic} [{message}]")]
    public static partial void DIAMErrorResponseFailed(this ILogger logger, string? participantId, string? topic, string? message);
    [LoggerMessage(4, LogLevel.Error, "Failed to publish CS Number response {participantId} to {topic} [{message}]")]
    public static partial void DIAMResponseFailed(this ILogger logger, string? participantId, string? topic, string? message);
    [LoggerMessage(5, LogLevel.Information, "Publishing eServices notification to {participantId} CS: {CSNumber}")]
    public static partial void PublishingToEServices(this ILogger logger, string? participantId, string? CSNumber);
}
