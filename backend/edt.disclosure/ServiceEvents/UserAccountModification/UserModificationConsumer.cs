namespace edt.disclosure.ServiceEvents.UserAccountModification;

using System.Net;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.ServiceEvents.UserAccountModification.Models;

public class UserModificationConsumer : BackgroundService
{

    private readonly IKafkaConsumer<string, UserChangeModel> consumer;

    private readonly EdtDisclosureServiceConfiguration config;
    public UserModificationConsumer(IKafkaConsumer<string, UserChangeModel> kafkaConsumer, EdtDisclosureServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        try
        {
            Serilog.Log.Information($"Consume from ConsumerTopicName {this.config.KafkaCluster.UserModificationTopicName}");
            await this.consumer.Consume(this.config.KafkaCluster.UserModificationTopicName, stoppingToken);
        }
        catch (Exception ex)
        {
            var errStr = $"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.UserModificationTopicName}, {ex}";
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
