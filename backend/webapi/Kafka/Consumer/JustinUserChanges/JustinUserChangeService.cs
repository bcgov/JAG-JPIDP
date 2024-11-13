namespace Pidp.Kafka.Consumer.JustinUserChanges;

using System.Net;
using Pidp.Kafka.Interfaces;

public class JustinUserChangeService(IKafkaConsumer<string, JustinUserChangeEvent> kafkaConsumer, PidpConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information("Starting consumer {0}", config.KafkaCluster.IncomingChangeEventTopic);

            await kafkaConsumer.Consume(config.KafkaCluster.IncomingChangeEventTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {config.KafkaCluster.ConsumerTopicName}, {ex}");
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
