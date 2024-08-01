namespace DIAMCornetService.Infrastructure;

using System.Globalization;
using Common.Kafka;
using Common.Kafka.Deserializer;
using Confluent.Kafka;
using IdentityModel.Client;


public class KafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue> where TValue : class
{
    private readonly ConsumerConfig _config;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<KafkaConsumer<TKey, TValue>> _logger;
    private IKafkaHandler<TKey, TValue> _handler;
    private IConsumer<TKey, TValue> _consumer;
    private string _topic;

    public KafkaConsumer(ConsumerConfig config, IServiceScopeFactory serviceScopeFactory, ILogger<KafkaConsumer<TKey, TValue>> logger)
    {
        _config = config;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task Consume(string topic, CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        _logger.LogInformation("DIAM Cornet Starting consumer for topic {0}", topic);

        _handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<TKey, TValue>>();
        _consumer = new ConsumerBuilder<TKey, TValue>(_config)
            .SetLogHandler((consumer, log) => Console.WriteLine($"CON _______________________ {log}"))
            .SetErrorHandler((consumer, log) => Console.WriteLine($"CON ERR _______________________ {log}"))
            .SetOAuthBearerTokenRefreshHandler(OauthTokenRefreshCallback)
            .SetValueDeserializer(new DefaultKafkaDeserializer<TValue>())
            .Build();
        _topic = topic;

        await Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start consuming from {0}", _topic);
        _consumer.Subscribe(_topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);

                if (result != null)
                {
                    var consumerResult = await _handler.HandleAsync(_consumer.Name, result.Message.Key, result.Message.Value);

                    if (consumerResult.Status == TaskStatus.RanToCompletion && consumerResult.Exception == null)
                    {
                        _logger.LogInformation($"Marking complete {result.Message.Key}");

                        _consumer.Commit(result);
                    }
                    else
                    {
                        _logger.LogInformation($"Error processing message {result.Message.Key} {consumerResult.Exception}");
                    }
                }
                else
                {
                    _logger.LogInformation("No messages received");
                    continue;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                _logger.LogInformation("Consumer error on topic {0} [{1}]", _topic, e.Message);

                if (e.Error.IsFatal)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation("General consumer error on topic {0} [{1}]", _topic, e.Message);

                Console.WriteLine($"Unexpected error: {e}");
                break;
            }
        }
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }

    public void Close()
    {
        _consumer.Close();
    }

    private static async void OauthTokenRefreshCallback(IClient client, string config)
    {
        try
        {
            var settingsFile = DIAMCornetServiceConfiguration.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";

            var clusterConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(settingsFile)
                .Build();

            var tokenEndpoint = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerTokenEndpointUrl");
            var clientId = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerConsumerClientId");
            var clientSecret = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerConsumerClientSecret");

            clientSecret ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerConsumerClientSecret");
            clientId ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerConsumerClientId");
            tokenEndpoint ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerTokenEndpointUrl");
            var accessTokenClient = new HttpClient();

            var accessToken = await accessTokenClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = "client_credentials"
            });

            var tokenTicks = GetTokenExpirationTime(accessToken.AccessToken);
            var subject = GetTokenSubject(accessToken.AccessToken);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks);
            var timeSpan = new DateTime() - tokenDate;
            var ms = tokenDate.ToUnixTimeMilliseconds();

            client.OAuthBearerSetToken(accessToken.AccessToken, ms, subject);
        }
        catch (Exception ex)
        {
            client.OAuthBearerSetTokenFailure(ex.ToString());
        }
    }

    private static long GetTokenExpirationTime(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);

        var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals("exp", StringComparison.Ordinal)).Value;
        var ticks = long.Parse(tokenExp, CultureInfo.InvariantCulture);
        return ticks;
    }

    private static string GetTokenSubject(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);
        return jwtSecurityToken.Claims.First(claim => claim.Type.Equals("sub", StringComparison.Ordinal)).Value;
    }
}
