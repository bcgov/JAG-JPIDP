namespace edt.service.ServiceEvents.UserAccountModification;

using System.Net;
using edt.service;
using edt.service.Kafka.Interfaces;
using edt.service.ServiceEvents.UserAccountModification.Models;

public class EdtUserModificationServiceConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, IncomingUserModification> consumer;

    private readonly EdtServiceConfiguration config;
    public EdtUserModificationServiceConsumer(IKafkaConsumer<string, IncomingUserModification> kafkaConsumer, EdtServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await this.consumer.Consume(this.config.KafkaCluster.IncomingUserChangeTopic, stoppingToken);
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

