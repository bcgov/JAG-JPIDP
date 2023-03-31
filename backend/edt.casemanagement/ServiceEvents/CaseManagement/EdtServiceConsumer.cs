namespace edt.casemanagement.ServiceEvents.CaseManagement;

using System.Net;
using edt.casemanagement.Kafka.Interfaces;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;

public class EdtServiceConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, SubAgencyDomainEvent> consumer;

    private readonly EdtServiceConfiguration config;
    public EdtServiceConsumer(IKafkaConsumer<string, SubAgencyDomainEvent> kafkaConsumer, EdtServiceConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Serilog.Log.Information("Starting consumer {0}", this.config.KafkaCluster.CaseAccessRequestTopicName);
        try
        {
            await this.consumer.Consume(this.config.KafkaCluster.CaseAccessRequestTopicName, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.CaseAccessRequestTopicName}, {ex}");
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
