using NotificationService.Kafka.Interfaces;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using Serilog;
using System.Net;

namespace NotificationService.NotificationEvents.UserProvisioning;
public class NotificationServiceConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, Notification> consumer;
    private readonly NotificationServiceConfiguration config;
    public NotificationServiceConsumer(IKafkaConsumer<string, Notification> kafkaConsumer, NotificationServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Log.Logger.Information("### Starting consumer from {0}", this.config.KafkaCluster.TopicName);
            await this.consumer.Consume(this.config.KafkaCluster.TopicName, stoppingToken);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.TopicName}, {ex}");
        }
    }

    public void Close() => this.consumer.Close();

    public override void Dispose()
    {
        this.consumer.Close();
        this.consumer.Dispose();

        base.Dispose();
    }
}

