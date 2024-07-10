namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using Common.Kafka;
using Confluent.Kafka;
using DIAMCornetService.Exceptions;
using DIAMCornetService.Models;

public class CornetService(IKafkaProducer<string, ParticipantResponseModel> producer, DIAMCornetServiceConfiguration cornetServiceConfiguration, ICornetORDSClient cornetORDSClient, ILogger<CornetService> logger) : ICornetService
{

    /// <summary>
    /// Publish message to DIAM topic
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="DIAMKafkaException"></exception>
    public async Task<ParticipantResponseModel> PublishNotificationToDIAMAsync(ParticipantResponseModel model)
    {
        try
        {
            var guid = Guid.NewGuid();

            var response = await producer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.ParticipantCSNumberMappingTopic, guid.ToString(), model);

            if (response.Status != PersistenceStatus.Persisted)
            {
                logger.LogError($"Failed to publish CS number lookup to topic for {model.ParticipantId}");
                throw new DIAMKafkaException($"Failed to publish cs number mapping for {guid.ToString()} Part: {model.ParticipantId} CSNumber: {model.CSNumber}");
            }
            else
            {
                model.DIAMPublishId = guid.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to publish CS number lookup to topic for {model.ParticipantId} [{ex.Message}]");
            throw;
        }

        return model;
    }


    public async Task<ParticipantResponseModel> PublishErrorsToDIAMAsync(ParticipantResponseModel model)
    {
        try
        {
            var guid = Guid.NewGuid();

            var response = await producer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic, guid.ToString(), model);

            if (response.Status != PersistenceStatus.Persisted)
            {
                logger.LogError($"Failed to publish Error response for {model.ParticipantId} to {cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic}");
                throw new DIAMKafkaException($"Failed to publish Error response for {model.ParticipantId} to {cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic}");
            }
            else
            {
                model.DIAMPublishId = guid.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to publish Error response for {model.ParticipantId} to {cornetServiceConfiguration.KafkaCluster.ProcessResponseTopic} [{ex.Message}]");
            throw;
        }

        return model;

    }

    /// <summary>
    /// Simple CS Number lookup
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public async Task<ParticipantResponseModel> LookupCSNumberForParticipant(string participantId)
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
    public async Task<ParticipantResponseModel> SubmitNotificationToEServices(ParticipantResponseModel model, string message)
    {
        logger.Log(LogLevel.Information, new EventId(), $"Publish notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message}", null, (state, ex) => state.ToString());

        var response = await cornetORDSClient.SubmitParticipantNotificationAsync(model, message);

        switch (response)
        {
            case "ok":
            {
                logger.Log(LogLevel.Information, new EventId(), $"Successfully published notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message}", null, (state, ex) => state.ToString());
                break;
            }
            case "NABI":
            {
                logger.LogInformation($"Failed to publish notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message} Reason: {response}");
                model.ErrorMessage = "Participant has no active BioMetrics";
                model.ErrorType = CornetCSNumberErrorType.noActiveBioMetrics;
                break;
            }
            case "ENDP":
            {
                logger.LogInformation($"Failed to publish notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message} Reason: {response}");
                model.ErrorMessage = "eDisclosure not provisioned for user";
                model.ErrorType = CornetCSNumberErrorType.eDisclosureNotProvisioned;
                break;
            }
            // shouldnt get to this one as CS number is looked up previously but added to cover all responses
            case "MISC":
            {
                logger.LogInformation($"Failed to publish notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message} Reason: {response}");
                model.ErrorMessage = "Missing CS Number";
                model.ErrorType = CornetCSNumberErrorType.missingCSNumber;
                break;
            }
            case "OTHR":
            {
                logger.LogInformation($"Failed to publish notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message} Reason: {response}");
                model.ErrorMessage = "Unknown CORNET error occurred";
                model.ErrorType = CornetCSNumberErrorType.otherError;
                break;
            }
            default:
            {
                logger.LogInformation($"Failed to publish notification for partId: {model.ParticipantId} CS: {model.CSNumber} {message} Reason: {response}");
                model.ErrorMessage = $"Unknown CORNET response {response}";
                model.ErrorType = CornetCSNumberErrorType.unknownResponseError;
                break;
            }
        }

        return model;

    }

    private static int GetRandomEightDigitNumber() => new Random().Next(10000000, 99999999);

}

