namespace Pidp.Kafka.Consumer.JustinParticipantMerges;

using System.Net;
using CommonModels.Models.JUSTIN;
using Pidp.Kafka.Interfaces;

public class JustinParticipantMergeEventConsumer(ILogger<JustinParticipantMergeEventConsumer> logger, IKafkaConsumer<string, ParticipantMergeDetailModel> kafkaConsumer, PidpConfiguration config) : BackgroundService
{
    private readonly IKafkaConsumer<string, ParticipantMergeDetailModel> consumer = kafkaConsumer;

    private readonly PidpConfiguration config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation($"Starting consumer from {this.config.KafkaCluster.ParticipantMergeResponseTopic}");

            await this.consumer.Consume(this.config.KafkaCluster.ParticipantMergeResponseTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.ParticipantMergeResponseTopic}, {ex.Message}");
        }
    }

    public override void Dispose()
    {
        this.consumer.Close();
        this.consumer.Dispose();

        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
