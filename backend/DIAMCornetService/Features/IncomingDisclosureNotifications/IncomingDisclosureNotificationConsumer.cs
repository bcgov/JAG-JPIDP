namespace DIAMCornetService.Features.MessageConsumer;


using System;
using System.Threading;
using Confluent.Kafka;

public class IncomingDisclosureNotificationConsumer(ILogger<IncomingDisclosureNotificationConsumer> logger, ConsumerConfig config, DIAMCornetServiceConfiguration configuration)
{


    public void StartConsuming(CancellationToken cancellationToken)
    {


        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            // subscribe to this topic
            consumer.Subscribe(configuration.KafkaCluster.ParticipantCSNumberMappingTopic);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);

                    // Handle the message, e.g., process consumeResult.Message
                    logger.LogInformation($"Received message: {consumeResult.Message.Value}");
                }
            }
            catch (OperationCanceledException)
            {
                // Ensure the consumer leaves the group cleanly and final offsets are committed.
                consumer.Close();
            }
        }
    }
}

