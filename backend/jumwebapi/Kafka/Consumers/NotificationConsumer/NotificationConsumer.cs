namespace jumwebapi.Kafka.Consumers.NotificationConsumer;


using Confluent.Kafka;
using global::Common.Kafka;
using Serilog;

public class NotificationConsumer<TKey, TValue>(ConsumerConfig config, IKafkaHandler<TKey, TValue> handler, IConsumer<TKey, TValue> consumer, string topic, IServiceScopeFactory serviceScopeFactory) : IKafkaConsumer<TKey, TValue> where TValue : class
{

    public async Task Consume(string topic, CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<TKey, TValue>>();
        consumer = new ConsumerBuilder<TKey, TValue>(config)
            // fix annoying logging
            .SetLogHandler((producer, log) => { })
            .SetErrorHandler((producer, log) => Log.Error($"Kafka error {log}"))
            .SetValueDeserializer(new KafkaDeserializer<TValue>()).Build();

        await Task.Run(() => this.StartConsumerLoop(stoppingToken), stoppingToken);
    }
    /// <summary>
    /// This will close the consumer, commit offsets and leave the group cleanly.
    /// </summary>
    public void Close()
    {
        consumer.Close();
    }
    /// <summary>
    /// Releases all resources used by the current instance of the consumer
    /// </summary>
    public void Dispose()
    {
        consumer.Dispose();
    }
    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(cancellationToken);

                if (result != null)
                {
                    await handler.HandleAsync(consumer.MemberId, result.Message.Key, result.Message.Value);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                // Consumer errors should generally be ignored (or logged) unless fatal.
                Console.WriteLine($"Consume error: {e.Error.Reason}");

                if (e.Error.IsFatal)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e}");
                break;
            }
        }
    }
}
