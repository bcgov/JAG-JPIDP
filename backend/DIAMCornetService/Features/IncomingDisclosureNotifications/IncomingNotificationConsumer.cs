namespace DIAMCornetService.Features.IncomingDisclosureNotifications;

using System.Net;
using DIAMCornetService.Infrastructure;
using DIAMCornetService.Models;
public class IncomingNotificationConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, IncomingDisclosureNotificationModel> consumer;

    private readonly DIAMCornetServiceConfiguration config;
    public IncomingNotificationConsumer(IKafkaConsumer<string, IncomingDisclosureNotificationModel> kafkaConsumer, DIAMCornetServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Serilog.Log.Information("Starting consumer {0}", this.config.KafkaCluster.DisclosureNotificationTopic);
        try
        {
            await this.consumer.Consume(this.config.KafkaCluster.DisclosureNotificationTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.DisclosureNotificationTopic}, {ex}");
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
