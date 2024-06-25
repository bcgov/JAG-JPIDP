namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using Common.Kafka;
using Confluent.Kafka;
using DIAMCornetService.Exceptions;
using DIAMCornetService.Models;

public class CornetService(ILogger<NotificationService> logger, IKafkaProducer<string, ParticipantCSNumberModel> producer, DIAMCornetServiceConfiguration cornetServiceConfiguration, ICornetORDSClient cornetORDSClient) : ICornetService
{

    /// <summary>
    /// To be implemented fully JAMAL
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public async Task<ParticipantCSNumberModel> GetParticipantCSNumberAsync(string participantId)
    {

        // JAMAL Code here - if this cannot lookup the participant, it should throw an exception (CornetException)
        logger.LogInformation($"*************************** ADD CS NUMBER LOOKUP CODE HERE ***************************");

        return new ParticipantCSNumberModel
        {
            ParticipantId = participantId,
            CSNumber = GetRandomEightDigitNumber().ToString()
        };

    }

    /// <summary>
    /// Publish a CS number to Participant ID response mapping to outbound topic
    /// </summary>
    /// <param name="participantId"></param>
    /// <param name="csNumber"></param>
    /// <returns></returns>
    /// <exception cref="DIAMKafkaException"></exception>
    public async Task<Dictionary<string, string>> PublishCSNumberResponseAsync(string participantId)
    {
        try
        {
            var guid = Guid.NewGuid();
            var mapping = await this.GetParticipantCSNumberAsync(participantId);

            var response = await producer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.ParticipantCSNumberMappingTopic, guid.ToString(), mapping);


            if (response.Status != PersistenceStatus.Persisted)
            {
                throw new DIAMKafkaException($"Failed to publish cs number mapping for {guid.ToString()} Part: {mapping.ParticipantId} CSNumber: {mapping.CSNumber}");
            }
            else
            {

                return new Dictionary<string, string>
                {
                    { "id", guid.ToString() },
                    { "CSNumber", string.IsNullOrEmpty( mapping.CSNumber) ? "NOT_FOUND": mapping.CSNumber },
                  };
            }
        }

        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish cs number mapping");
            throw;
        }

    }


    /// <summary>
    /// Publish a notification within the CORNET system
    /// </summary>
    /// <param name="csNumber"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<int> PublishNotificationAsync(string csNumber, string message)
    {
        logger.LogInformation($"Publish notification for {csNumber} {message}");


        // JAMAL add code here
        logger.LogInformation($"*************************** ADD PUBLISH EVENT CODE HERE ***************************");

        // some response code to show notification worked
        return 0;

    }

    private static int GetRandomEightDigitNumber() => new Random().Next(10000000, 99999999);
}
