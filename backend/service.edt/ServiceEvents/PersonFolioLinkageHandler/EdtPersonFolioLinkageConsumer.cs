namespace edt.service.ServiceEvents.DefenceParticipantCreation;

using System.Net;
using Common.Models.EDT;
using edt.service.Kafka.Interfaces;

public class EdtPersonFolioLinkageConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, PersonFolioLinkageModel> consumer;

    private readonly EdtServiceConfiguration config;
    public EdtPersonFolioLinkageConsumer(IKafkaConsumer<string, PersonFolioLinkageModel> kafkaConsumer, EdtServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information($"Consume from CoreFolioCreationNotificationTopic [{this.config.KafkaCluster.CoreFolioCreationNotificationTopic}]");
            await this.consumer.Consume(this.config.KafkaCluster.CoreFolioCreationNotificationTopic, stoppingToken);
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

