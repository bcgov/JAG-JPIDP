namespace JAMService.Infrastructure.Kafka;

using System.Net;
using Common.Kafka;
using CommonModels.Models.JUSTIN;

public class IncomingJamProvisioningConsumer(ILogger<IncomingJamProvisioningConsumer> logger, IKafkaConsumer<string, JAMProvisioningRequestModel> kafkaConsumer, JAMServiceConfiguration config) : BackgroundService
{


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        logger.LogInformation("Starting consumer {0}", config.KafkaCluster.IncomingJamProvisioningTopic);
        try
        {
            await kafkaConsumer.Consume(config.KafkaCluster.IncomingJamProvisioningTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {config.KafkaCluster.IncomingJamProvisioningTopic}, {ex}");
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
