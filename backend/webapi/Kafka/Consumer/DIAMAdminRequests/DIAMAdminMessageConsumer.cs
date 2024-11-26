namespace Pidp.Kafka.Consumer.DIAMAdminRequests;

using System.Net;
using CommonModels.Models.DIAMAdmin;
using Pidp.Kafka.Interfaces;

public class DIAMAdminMessageConsumer(IKafkaConsumer<string, AdminRequestModel> kafkaConsumer, PidpConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information("Starting consumer {0}", config.KafkaCluster.DIAMAdminIncomingTopic);

            await kafkaConsumer.Consume(config.KafkaCluster.DIAMAdminIncomingTopic, stoppingToken);
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
