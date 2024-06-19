namespace DIAMCornetService.Features.MessageConsumer;


using System;
using System.Threading;
using Confluent.Kafka;

public class IncomingDisclosureNotificationConsumer(ConsumerConfig config, IServiceScopeFactory serviceScopeFactory, DIAMCornetServiceConfiguration configuration)
{
    private readonly IServiceScopeFactory serviceScopeFactory = serviceScopeFactory;
    private readonly ConsumerConfig config = config;
    private readonly DIAMCornetServiceConfiguration configuration = configuration;

    public void StartConsuming(CancellationToken cancellationToken)
    {


        using (var consumer = new ConsumerBuilder<Ignore, string>(this.config).Build())
        {
            // subscribe to this topic
            consumer.Subscribe(this.configuration.KafkaCluster.ParticipantCSNumberMappingTopic);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);

                    // Handle the message, e.g., process consumeResult.Message
                    Console.WriteLine($"Received message: {consumeResult.Message.Value}");
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

