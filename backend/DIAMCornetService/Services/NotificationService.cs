namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using Common.Kafka;
using Confluent.Kafka;
using DIAMCornetService.Exceptions;
using DIAMCornetService.Models;

public class NotificationService(ILogger<NotificationService> logger, IKafkaProducer<string, IncomingDisclosureNotificationModel> producer, DIAMCornetServiceConfiguration cornetServiceConfiguration) : INotificationService
{
    public async Task<string> PublishTestNotificationAsync(string participantId, string messageText)
    {
        try
        {
            var guid = Guid.NewGuid();
            var response = await producer.ProduceAsync(cornetServiceConfiguration.KafkaCluster.DisclosureNotificationTopic, guid.ToString(), new IncomingDisclosureNotificationModel
            {
                ParticipantId = participantId,
                NotificationDateUTC = DateTime.UtcNow,
                MessageText = messageText
            });

            if (response.Status != PersistenceStatus.Persisted)
            {
                throw new DIAMKafkaException($"Failed to publish notification for {guid.ToString()} Part: {participantId}");
            }
            else
            {
                return guid.ToString();
            }


        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish test notification");
            throw;
        }
    }
}
