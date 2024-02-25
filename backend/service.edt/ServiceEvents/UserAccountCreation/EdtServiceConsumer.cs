namespace edt.service.ServiceEvents.UserAccountCreation;

using System.Net;
using edt.service;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka.Interfaces;

public class EdtServiceConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, EdtUserProvisioningModel> consumer;

    private readonly EdtServiceConfiguration config;
    public EdtServiceConsumer(IKafkaConsumer<string, EdtUserProvisioningModel> kafkaConsumer, EdtServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Serilog.Log.Information($"Consume from Access Request Topic [{this.config.KafkaCluster.ConsumerTopicName}]");
            await this.consumer.Consume(this.config.KafkaCluster.ConsumerTopicName, stoppingToken);
        }
        catch (Exception ex)
        {
            var errStr = $"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.ConsumerTopicName}, {ex}";
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

