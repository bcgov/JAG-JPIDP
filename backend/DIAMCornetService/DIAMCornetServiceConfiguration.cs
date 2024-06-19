namespace DIAMCornetService;

public class DIAMCornetServiceConfiguration
{

    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public CornetConfiguration CornetService { get; set; } = new();

    public class KafkaClusterConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string BootstrapServers { get; set; } = string.Empty;
        public string ParticipantCSNumberMappingTopic { get; set; } = string.Empty;
        public string DisclosureNotificationTopic { get; set; } = string.Empty;
        public string ProcessResponseTopic { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "diam-cornet-con-group";

    }

    public class ConnectionStringConfiguration
    {
        public string DIAMCornetDatabase { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
    }

    public class CornetConfiguration
    {
        public string BaseAddress { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

}
