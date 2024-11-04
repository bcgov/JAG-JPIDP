namespace jumwebapi.Kafka.Consumers.NotificationConsumer;


using Confluent.Kafka;
using jumwebapi.Kafka.Interfaces;
using jumwebapi.Kafka.Producer.Interfaces;
using Serilog;

public class ParticipantMergeConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue> where TValue : class
{
    private readonly ConsumerConfig _config;
    private IKafkaHandler<TKey, TValue> _handler;
    private IConsumer<TKey, TValue> _consumer;
    private string _topic;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ParticipantMergeConsumer(ConsumerConfig config, IKafkaHandler<TKey, TValue> handler, IConsumer<TKey, TValue> consumer, string topic, IServiceScopeFactory serviceScopeFactory)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._config = config;
        this._consumer = consumer;
        this._handler = handler;
        this._topic = topic;
    }

    public async Task Consume(string topic, CancellationToken stoppingToken)
    {
        using var scope = this._serviceScopeFactory.CreateScope();

        this._handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<TKey, TValue>>();
        this._consumer = new ConsumerBuilder<TKey, TValue>(this._config)
            // fix annoying logging
            .SetLogHandler((producer, log) => { })
            .SetErrorHandler((producer, log) => Log.Error($"Kafka error {log}"))
            .SetValueDeserializer(new KafkaDeserializer<TValue>()).Build();
        this._topic = topic;

        await Task.Run(() => this.StartConsumerLoop(stoppingToken), stoppingToken);
    }
    /// <summary>
    /// This will close the consumer, commit offsets and leave the group cleanly.
    /// </summary>
    public void Close()
    {
        this._consumer.Close();
    }
    /// <summary>
    /// Releases all resources used by the current instance of the consumer
    /// </summary>
    public void Dispose()
    {
        this._consumer.Dispose();
    }
    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        this._consumer.Subscribe(this._topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = this._consumer.Consume(cancellationToken);

                if (result != null)
                {
                    await this._handler.HandleAsync(result.Message.Key, result.Message.Value);
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
