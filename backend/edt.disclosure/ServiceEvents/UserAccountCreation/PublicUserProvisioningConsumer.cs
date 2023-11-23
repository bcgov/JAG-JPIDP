using System.Net;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;

namespace edt.disclosure.ServiceEvents.UserAccountCreation;

public class PublicUserProvisioningConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, EdtDisclosureUserProvisioningModel> consumer;

    private readonly EdtDisclosureServiceConfiguration config;
    public PublicUserProvisioningConsumer(IKafkaConsumer<string, EdtDisclosureUserProvisioningModel> kafkaConsumer, EdtDisclosureServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        try
        {
            Serilog.Log.Information($"Consume from Public User Topic {this.config.KafkaCluster.CreatePublicUserTopic}");
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
