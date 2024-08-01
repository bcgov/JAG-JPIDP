namespace DIAMCornetService.Features.IncomingDisclosureNotifications;

using System.Net;
using DIAMCornetService.Infrastructure;
using DIAMCornetService.Models;

public class IncomingNotificationConsumer(ILogger<IncomingNotificationConsumer> logger, IKafkaConsumer<string, IncomingDisclosureNotificationModel> kafkaConsumer, DIAMCornetServiceConfiguration config) : BackgroundService
{


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        logger.LogInformation("Starting consumer {0}", config.KafkaCluster.DisclosureNotificationTopic);
        try
        {
            await kafkaConsumer.Consume(config.KafkaCluster.DisclosureNotificationTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {config.KafkaCluster.DisclosureNotificationTopic}, {ex}");
        }
    }

    public override void Dispose()
    {
        kafkaConsumer.Close();
        kafkaConsumer.Dispose();

        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
