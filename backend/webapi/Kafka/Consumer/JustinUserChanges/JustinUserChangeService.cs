namespace Pidp.Kafka.Consumer.JustinUserChanges;

using System.Net;
using Pidp.Kafka.Interfaces;

public class JustinUserChangeService : BackgroundService
{
    private readonly IKafkaConsumer<string, JustinUserChangeEvent> consumer;

    private readonly PidpConfiguration config;
    public JustinUserChangeService(IKafkaConsumer<string, JustinUserChangeEvent> kafkaConsumer, PidpConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
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
