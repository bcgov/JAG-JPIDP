namespace jumwebapi.Kafka.Consumers.ParticipantMergeConsumer;

using System.Net;
using global::Common.Kafka;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Models;

public class ParticipantMergeConsumerService(IKafkaConsumer<string, ParticipantMergeEvent> kafkaConsumer, JumWebApiConfiguration config) : BackgroundService
{
    private readonly IKafkaConsumer<string, ParticipantMergeEvent> consumer = kafkaConsumer;
    private readonly JumWebApiConfiguration config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information("ParticipantMergeConsumerService Starting consumer {0}", this.config.KafkaCluster.ParticipantMergeConsumeTopic);

            await this.consumer.Consume(this.config.KafkaCluster.ParticipantMergeConsumeTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.ParticipantMergeConsumeTopic}, {ex}");
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
