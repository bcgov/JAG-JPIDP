namespace Pidp.Kafka.Consumer.Notifications;

using System.Net;
using Pidp.Kafka.Interfaces;

public class NotificationAckService(IKafkaConsumer<string, NotificationAckModel> kafkaConsumer, PidpConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information("Starting consumer {0}", config.KafkaCluster.ConsumerTopicName);

            await kafkaConsumer.Consume(config.KafkaCluster.ConsumerTopicName, stoppingToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {config.KafkaCluster.ConsumerTopicName}, {ex}");
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
