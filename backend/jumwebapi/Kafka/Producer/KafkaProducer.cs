
namespace jumwebapi.Kafka.Producer;

using System.Globalization;
using Confluent.Kafka;
using IdentityModel.Client;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Kafka.Producer.Interfaces;
using Serilog;


public class KafkaProducer<TKey, TValue> : IDisposable, IKafkaProducer<TKey, TValue> where TValue : class
{
    private readonly IProducer<TKey, TValue> producer;
    private const string SUBJECT_CLAIM = "sub";
    private const string EXPIRY_CLAIM = "exp";


    public KafkaProducer(ProducerConfig config) => this.producer = new ProducerBuilder<TKey, TValue>(config)
        // fix annoying logging
        .SetLogHandler((producer, log) => { })
        .SetErrorHandler((producer, log) => Log.Error($"Kafka error {log}"))
        .SetOAuthBearerTokenRefreshHandler(this.OauthTokenRefreshCallback).SetValueSerializer(new KafkaSerializer<TValue>()).Build();



    public async Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, TKey key, TValue value)
    {
        var message = new Message<TKey, TValue> { Key = key, Value = value };

        var response = await this.producer.ProduceAsync(topic, message);
        Log.Information($"Producer response {topic} {response.Status} Partition: {response.Partition.Value}");
        return response;

    }


    public void Dispose()
    {
        this.producer.Flush();
        this.producer.Dispose();
        GC.SuppressFinalize(this);

    }

    private async void OauthTokenRefreshCallback(IClient client, string config)
    {
        try
        {

            var settingsFile = JumWebApiConfiguration.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";


            var clusterConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(settingsFile).Build();

            var tokenEndpoint = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerTokenEndpointUrl");
            var clientId = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerProducerClientId");
            var clientSecret = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerProducerClientSecret");

            clientSecret ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerProducerClientSecret");
            clientId ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerProducerClientId");
            tokenEndpoint ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerTokenEndpointUrl");

            Log.Logger.Debug("JUM Kafka Producer getting token {0} {1} {2}", tokenEndpoint, clientId, clientSecret);
            var accessTokenClient = new HttpClient();
            var accessToken = await accessTokenClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = "client_credentials"
            });
            var tokenTicks = GetTokenExpirationTime(accessToken.AccessToken);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks);
            var subject = GetTokenSubject(accessToken.AccessToken);
            var ms = tokenDate.ToUnixTimeMilliseconds();
            Log.Logger.Debug("Producer got token {0}", ms);

            client.OAuthBearerSetToken(accessToken.AccessToken, ms, subject);
        }
        catch (Exception ex)
        {
            Log.Logger.Error("### Token error {0}", ex.ToString());
            client.OAuthBearerSetTokenFailure(ex.ToString());
        }
    }
    private static long GetTokenExpirationTime(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);
        var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals(EXPIRY_CLAIM, StringComparison.Ordinal)).Value;
        var ticks = long.Parse(tokenExp, CultureInfo.InvariantCulture);
        return ticks;
    }

    private static string GetTokenSubject(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);
        return jwtSecurityToken.Claims.First(claim => claim.Type.Equals(SUBJECT_CLAIM, StringComparison.Ordinal)).Value;

    }
}
