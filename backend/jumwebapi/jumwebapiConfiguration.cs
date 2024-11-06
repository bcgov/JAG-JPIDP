namespace jumwebapi.Infrastructure.Auth;
public class JumWebApiConfiguration
{
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;

    public AddressAutocompleteClientConfiguration AddressAutocompleteClient { get; set; } = new();
    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public ChesClientConfiguration ChesClient { get; set; } = new();
    public JustinClientConfiguration JustinParticipantClient { get; set; } = new();
    public JustinClientConfiguration TestORDSConfiguration { get; set; } = new();
    public JustinBackgroundEventConfiguration JustinChangeEventClient { get; set; } = new();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    public JustinClientConfiguration JustinCaseClient { get; set; } = new();

    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public KeycloakConfiguration Keycloak { get; set; } = new();
    public MailServerConfiguration MailServer { get; set; } = new();
    public JustinClientAuthentication JustinAuthentication { get; set; } = new();

    // ------- Configuration Objects -------

    public class AddressAutocompleteClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }

    public class ConnectionStringConfiguration
    {
        public string JumDatabase { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
    }

    public class ChesClientConfiguration
    {
        public bool Enabled { get; set; }
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
    }
    public class KafkaClusterConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BootstrapServers { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string UserChangeEventTopic { get; set; } = string.Empty;
        public string UserChangeProcessedTopic { get; set; } = string.Empty;
        public string SecurityProtocol { get; set; } = string.Empty;
        public string SaslMechanism { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string HostnameVerification { get; set; } = "Https";
        public string ParticipantMergeTopic { get; set; } = string.Empty;
        public string ConsumerGroupId { get; set; } = "justin-service-consumer";

    }
    public class JustinClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string PollCron { get; set; } = "0 0 0-0 ? * * *";

    }

    public class JustinBackgroundEventConfiguration : JustinClientConfiguration
    {
        public int PollRateSeconds { get; set; } = 600; // default to every 10 minutes
    }

    public class JustinClientAuthentication
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;

        public string BasicAuthUsername { get; set; } = string.Empty;
        public string BasicAuthPassword { get; set; } = string.Empty;
    }
    public class KeycloakConfiguration
    {
        public string RealmUrl { get; set; } = string.Empty;
        public string AdministrationUrl { get; set; } = string.Empty;
        public string AdministrationClientId { get; set; } = string.Empty;
        public string AdministrationClientSecret { get; set; } = string.Empty;
        public string HcimClientId { get; set; } = string.Empty;
    }
    public class MailServerConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
