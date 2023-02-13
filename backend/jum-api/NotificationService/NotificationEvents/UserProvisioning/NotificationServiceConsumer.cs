using NotificationService.Kafka.Interfaces;
using NotificationService.NotificationEvents.UserProvisioning.Models;
using Serilog;
using System.Net;

namespace NotificationService.NotificationEvents.UserProvisioning;
public class NotificationServiceConsumer : BackgroundService
{
	private readonly IKafkaConsumer<string, Notification> _consumer;
	private readonly NotificationServiceConfiguration _config;
	public NotificationServiceConsumer(IKafkaConsumer<string, Notification> kafkaConsumer, NotificationServiceConfiguration config)
	{
		_consumer = kafkaConsumer;
		_config = config;
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			Log.Logger.Information("### Starting consumer from {0}", _config.KafkaCluster.TopicName);
			await _consumer.Consume(_config.KafkaCluster.TopicName, stoppingToken);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {_config.KafkaCluster.TopicName}, {ex}");
		}
	}

	public override void Dispose()
	{
		_consumer.Close();
		_consumer.Dispose();

		base.Dispose();
	}
}

