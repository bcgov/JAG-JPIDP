namespace edt.casemanagement.ServiceEvents.CourtLocation;

using System.Net;
using edt.casemanagement.Kafka.Interfaces;
using edt.casemanagement.ServiceEvents.CourtLocation.Models;

public class CourtLocationConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, CourtLocationDomainEvent> consumer;

    private readonly EdtServiceConfiguration config;
    public CourtLocationConsumer(IKafkaConsumer<string, CourtLocationDomainEvent> kafkaConsumer, EdtServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Serilog.Log.Information("Starting consumer {0}", this.config.KafkaCluster.CourtLocationAccessRequestTopic);
        try
        {
            await this.consumer.Consume(this.config.KafkaCluster.CourtLocationAccessRequestTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.CourtLocationAccessRequestTopic}, {ex}");
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
