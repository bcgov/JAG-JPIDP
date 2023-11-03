namespace ApprovalFlow.ServiceEvents.IncomingApproval;

using System.Net;
using Common.Kafka;
using Common.Models.Approval;

public class ApprovalConsumer : BackgroundService
{
    private readonly IKafkaConsumer<string, ApprovalRequestModel> consumer;

    private readonly ApprovalFlowConfiguration config;
    public ApprovalConsumer(IKafkaConsumer<string, ApprovalRequestModel> kafkaConsumer, ApprovalFlowConfiguration config)
    {
        this.consumer = kafkaConsumer;
        this.config = config;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        Serilog.Log.Information("Starting consumer {0}", this.config.KafkaCluster.IncomingApprovalCreationTopic);
        try
        {
            await this.consumer.Consume(this.config.KafkaCluster.IncomingApprovalCreationTopic, stoppingToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {this.config.KafkaCluster.IncomingApprovalCreationTopic}, {ex}");
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
