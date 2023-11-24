using System.Net;
using Common.Models.EDT;
using edt.disclosure.Kafka.Interfaces;

namespace edt.disclosure.ServiceEvents.UserAccountCreation;

public class PublicUserProvisioningConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, EdtDisclosurePublicUserProvisioningModel> consumer;

    private readonly EdtDisclosureServiceConfiguration config;
    public PublicUserProvisioningConsumer(IKafkaConsumer<string, EdtDisclosurePublicUserProvisioningModel> kafkaConsumer, EdtDisclosureServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        try
        {
            Serilog.Log.Information($"Starting consumer from Public User Topic {this.config.KafkaCluster.CreatePublicUserTopic}");
            await this.consumer.Consume(this.config.KafkaCluster.CreatePublicUserTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            var errStr = $"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.CreatePublicUserTopic}, {ex}";
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
