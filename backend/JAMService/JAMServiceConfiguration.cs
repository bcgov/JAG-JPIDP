namespace JAMService;

public class JAMServiceConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public ConnectionStringConfiguration DatabaseConnectionInfo { get; set; } = new();
    public KeycloakAdminConfiguration KeycloakConfiguration { get; set; } = new();
    public JustinClientAuthentication JustinApplicationRolesClient { get; set; } = new JustinClientAuthentication();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();

    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }


    public class KafkaClusterConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BootstrapServers { get; set; } = string.Empty;
        public string UserAccountChangeNotificationTopicName { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "dems-notification-ack";
        public string KafkaAdminClientId { get; set; } = string.Empty;
        public string KafkaAdminClientSecret { get; set; } = string.Empty;
        public string HostnameVerification { get; set; } = "Https";
        public string IncomingJamProvisioningTopic { get; set; } = string.Empty;
        public string ProcessResponseTopic { get; set; } = string.Empty;
        public string NotificationTopic { get; set; } = string.Empty;


    }

    public class KeycloakAdminConfiguration
    {
        public string KeycloakAdminClient { get; set; } = string.Empty;
        public string KeycloakAdminSecret { get; set; } = string.Empty;
        public string KeycloakAdminUser { get; set; } = string.Empty;
        public string KeycloakAdminPassword { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class ConnectionStringConfiguration
    {
        public string JAMServiceConnection { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
    }
    public class JustinClientAuthentication
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;

        public string BasicAuthUsername { get; set; } = string.Empty;
        public string BasicAuthPassword { get; set; } = string.Empty;
    }

}
