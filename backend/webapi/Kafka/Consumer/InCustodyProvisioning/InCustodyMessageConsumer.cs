namespace Pidp.Kafka.Consumer.InCustodyProvisioning;

using System.Net;
using Common.Models.CORNET;
using Pidp.Kafka.Interfaces;

public class InCustodyMessageConsumer(IKafkaConsumer<string, InCustodyParticipantModel> kafkaConsumer, PidpConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Serilog.Log.Information("Starting consumer {0}", config.KafkaCluster.ParticipantCSNumberMappingTopic);
        try
        {
            await kafkaConsumer.Consume(config.KafkaCluster.ParticipantCSNumberMappingTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {config.KafkaCluster.ParticipantCSNumberMappingTopic}, {ex}");
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
