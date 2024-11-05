namespace JAMService.Infrastructure.Kafka;

using System.Globalization;
using Common.Kafka;
using Common.Kafka.Serializer;
using Confluent.Kafka;
using IdentityModel.Client;
using Serilog;

public class KafkaProducer<TKey, TValue>(ProducerConfig config) : IDisposable, IKafkaProducer<TKey, TValue> where TValue : class
{
    private readonly IProducer<TKey, TValue> producer = new ProducerBuilder<TKey, TValue>(config)
        // fix annoying logging
        .SetLogHandler((producer, log) => { })
        .SetErrorHandler((producer, log) => Log.Error($"Kafka error {log}"))
        .SetOAuthBearerTokenRefreshHandler(OauthTokenRefreshCallback).SetValueSerializer(new KafkaSerializer<TValue>()).Build();


    private const string EXPIRY_CLAIM = "exp";
    private const string SUBJECT_CLAIM = "sub";



    public async Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, TKey key, TValue value)
    {
        var message = new Message<TKey, TValue> { Key = key, Value = value };
        try
        {
            return await this.producer.ProduceAsync(topic, message);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to sent to topic [{topic}] [{ex.Message}]");
            throw;
        }
        finally
        {
            //    activity?.Stop();
        }
    }
    public void Dispose()
    {
        this.producer.Flush();
        this.producer.Dispose();
        GC.SuppressFinalize(this);
    }
    private static async void OauthTokenRefreshCallback(IClient client, string config)
    {
        try
        {

            var settingsFile = JAMServiceConfiguration.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";


            var clusterConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(settingsFile).Build();

            var tokenEndpoint = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerTokenEndpointUrl");
            var clientId = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerProducerClientId");
            var clientSecret = Environment.GetEnvironmentVariable("KafkaCluster__SaslOauthbearerProducerClientSecret");

            clientSecret ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerProducerClientSecret");
            clientId ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerProducerClientId");
            tokenEndpoint ??= clusterConfig.GetValue<string>("KafkaCluster:SaslOauthbearerTokenEndpointUrl");

            Log.Logger.Debug("DIAM Cornet Kafka Producer getting token {0} {1}", tokenEndpoint, clientId);

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
            Log.Logger.Debug("Producer got token {0}", ms);

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

        var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals(KafkaProducer<TKey, TValue>.EXPIRY_CLAIM, StringComparison.Ordinal)).Value;
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
