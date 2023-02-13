using System.Globalization;
using Confluent.Kafka;
using IdentityModel.Client;
using jumwebapi.Kafka.Producer.Interfaces;
using Serilog;

namespace jumwebapi.Kafka.Producer;
public class KafkaProducer<TKey, TValue> : IDisposable, IKafkaProducer<TKey, TValue> where TValue : class
{
    private readonly IProducer<TKey, TValue> _producer;
    private const string SUBJECT_CLAIM = "sub";
    private const string EXPIRY_CLAIM = "exp";


    public KafkaProducer(ProducerConfig config)
    {
        _producer = new ProducerBuilder<TKey, TValue>(config).SetOAuthBearerTokenRefreshHandler(OauthTokenRefreshCallback).SetValueSerializer(new KafkaSerializer<TValue>()).Build();
    }
    public async Task ProduceAsync(string topic, TKey key, TValue value)
    {
        await _producer.ProduceAsync(topic, new Message<TKey, TValue> { Key = key, Value = value });
    }
    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }

    private async void OauthTokenRefreshCallback(IClient client, string config)
    {
        try
        {
            var clusterConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var tokenEndpoint = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerTokenEndpointUrl");
            var clientId = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerProducerClientId");
            var clientSecret = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerProducerClientSecret");

            clientSecret ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerProducerClientSecret");
            clientId ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerProducerClientId");
            tokenEndpoint ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerTokenEndpointUrl");

            Log.Logger.Information("JUM Kafka Producer getting token {0} {1} {2}", tokenEndpoint, clientId, clientSecret);
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
            Log.Logger.Information("Producer got token {0}", ms);

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
