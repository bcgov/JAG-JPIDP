using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using System.Net;

namespace edt.disclosure.ServiceEvents.UserAccountCreation;

public class UserProvisioningConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, EdtDisclosureUserProvisioningModel> consumer;

    private readonly EdtDisclosureServiceConfiguration config;
    public UserProvisioningConsumer(IKafkaConsumer<string, EdtDisclosureUserProvisioningModel> kafkaConsumer, EdtDisclosureServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        try
        {
            Serilog.Log.Information($"Consume from ConsumerTopicName {this.config.KafkaCluster.CreateUserTopic}");
            await this.consumer.Consume(this.config.KafkaCluster.CreateUserTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            var errStr = $"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.CreateUserTopic}, {ex}";
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
