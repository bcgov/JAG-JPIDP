namespace edt.service.ServiceEvents.DefenceParticipantCreation;

using System.Net;
using Common.Models.EDT;
using edt.service.Kafka.Interfaces;

public class EdtPersonCreationConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, EdtPersonProvisioningModel> consumer;

    private readonly EdtServiceConfiguration config;
    public EdtPersonCreationConsumer(IKafkaConsumer<string, EdtPersonProvisioningModel> kafkaConsumer, EdtServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information($"Consume from PersonCreationTopic [{this.config.KafkaCluster.PersonCreationTopic}]");
            await this.consumer.Consume(this.config.KafkaCluster.PersonCreationTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            var errStr = $"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.PersonCreationTopic}, {ex}";
            Serilog.Log.Warning(errStr);
            Console.WriteLine(errStr);
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

