using System.Globalization;
using Confluent.Kafka;
using IdentityModel.Client;
using NotificationService.Kafka.Interfaces;
using Serilog;

namespace NotificationService.Kafka;
public class KafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue> where TValue : class
{
    private readonly ConsumerConfig _config;
    private IKafkaHandler<TKey, TValue> _handler;
    private IConsumer<TKey, TValue> _consumer;
    private string _topic;
    private const string EXPIRY_CLAIM = "exp";
    private const string SUBJECT_CLAIM = "sub";


    private readonly IServiceScopeFactory _serviceScopeFactory;

    public KafkaConsumer(ConsumerConfig config, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _config = config;
    }

    public async Task Consume(string topic, CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        _handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<TKey, TValue>>();
        _consumer = new ConsumerBuilder<TKey, TValue>(_config).SetOAuthBearerTokenRefreshHandler(OauthTokenRefreshCallback).SetValueDeserializer(new KafkaDeserializer<TValue>()).Build();
        _topic = topic;

        await Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }
    /// <summary>
    /// This will close the consumer, commit offsets and leave the group cleanly.
    /// </summary>
    public void Close()
    {
        _consumer.Close();
    }
    /// <summary>
    /// Releases all resources used by the current instance of the consumer
    /// </summary>
    public void Dispose()
    {
        _consumer.Dispose();
    }
    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        Log.Logger.Information("starting consumer from topic  {0}", _topic);


        _consumer.Subscribe(_topic);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);
                if (result != null)
                {

                    Log.Logger.Information("Received message {0}", result.Message.Key);
                    var consumerResult =  await _handler.HandleAsync(_consumer.Name, result.Message.Key, result.Message.Value);

                    if (consumerResult.Status == TaskStatus.RanToCompletion && consumerResult.Exception == null)
                    {
                        _consumer.Commit(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                // Consumer errors should generally be ignored (or logged) unless fatal.
                Log.Logger.Warning($"Consume error: {e.Error.Reason}");

                if (e.Error.IsFatal)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error($"Unexpected error: {e}");
                break;
            }
        }
    }

    private static async void OauthTokenRefreshCallback(IClient client, string config)
    {
        try
        {

            var settingsFile = NotificationServiceConfiguration.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";

            var clusterConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(settingsFile).Build();

            var tokenEndpoint = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerTokenEndpointUrl");
            var clientId = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerConsumerClientId");
            var clientSecret = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerConsumerClientSecret");

            clientSecret ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerConsumerClientSecret");
            clientId ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerConsumerClientId");
            tokenEndpoint ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerTokenEndpointUrl");

            Log.Logger.Debug("Jum Kafka Consumer getting token {0} {1}", tokenEndpoint, clientId);
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
            Log.Logger.Debug("Consumer got token {0}", ms);

            client.OAuthBearerSetToken(accessToken.AccessToken, ms, subject);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            client.OAuthBearerSetTokenFailure(ex.ToString());
        }
    }
    private static long GetTokenExpirationTime(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);

        var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals(KafkaConsumer<TKey, TValue>.EXPIRY_CLAIM, StringComparison.Ordinal)).Value;
        var ticks = long.Parse(tokenExp, CultureInfo.InvariantCulture);
        return ticks;
    }

    private static string GetTokenSubject(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);
        return jwtSecurityToken.Claims.First(claim => claim.Type.Equals(KafkaConsumer<TKey, TValue>.SUBJECT_CLAIM, StringComparison.Ordinal)).Value;

    }
}

