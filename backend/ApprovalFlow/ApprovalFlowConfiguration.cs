namespace ApprovalFlow;
public class ApprovalFlowConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public ApprovalConfiguration ApprovalConfig { get; set; } = new();
    public KeycloakConfiguration Keycloak { get; set; } = new();

    public SchemaRegistryConfiguration SchemaRegistry { get; set; } = new();
    public TelemeteryConfiguration Telemetry { get; set; } = new TelemeteryConfiguration();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();


    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }

    public class ApprovalConfiguration
    {
        public string NotifyEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }

    // ------- Configuration Objects -------

    public class TelemeteryConfiguration
    {
        public string CollectorUrl { get; set; } = string.Empty;
        public string AzureConnectionString { get; set; } = string.Empty;
        public bool LogToConsole { get; set; }

    }


    public class ConnectionStringConfiguration
    {
        public string ApprovalFlowDataStore { get; set; } = string.Empty;
        public string Schema { get; set; } = "approvalflow";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
    }

    public class KeycloakConfiguration
    {
        public string RealmUrl { get; set; } = string.Empty;
        public string AdministrationUrl { get; set; } = string.Empty;
        public string AdministrationClientId { get; set; } = string.Empty;
        public string AdministrationClientSecret { get; set; } = string.Empty;
        public string HcimClientId { get; set; } = string.Empty;
    }



    public class SchemaRegistryConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

    }


    public class KafkaClusterConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string BootstrapServers { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string IncomingApprovalCreationTopic { get; set; } = string.Empty;
        public string ApprovalResponseTopic { get; set; } = string.Empty;
        public string NotificationTopic { get; set; } = string.Empty;

        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "approval-consumer-group";


    }

}

