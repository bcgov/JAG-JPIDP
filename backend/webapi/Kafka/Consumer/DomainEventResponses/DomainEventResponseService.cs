namespace Pidp.Kafka.Consumer.DomainEventResponses;

using System.Net;
using Pidp.Kafka.Interfaces;
using Pidp.Models;

public class DomainEventResponseService : BackgroundService
{
    private readonly IKafkaConsumer<string, GenericProcessStatusResponse> consumer;

    private readonly PidpConfiguration config;
    public DomainEventResponseService(IKafkaConsumer<string, GenericProcessStatusResponse> kafkaConsumer, PidpConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information("Starting consumer {0}", this.config.KafkaCluster.ProcessResponseTopic);

            await this.consumer.Consume(this.config.KafkaCluster.IncomingChangeEventTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.ConsumerTopicName}, {ex}");
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
