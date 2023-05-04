namespace edt.casemanagement;


using System.Text.Json;
using System.Transactions;
using edt.casemanagement.Infrastructure.Auth;

public class EdtServiceConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public ChesClientConfiguration ChesClient { get; set; } = new();
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public MailServerConfiguration MailServer { get; set; } = new();
    public EdtClientConfiguration EdtClient { get; set; } = new();
    public KeycloakConfiguration Keycloak { get; set; } = new();

    public SchemaRegistryConfiguration SchemaRegistry { get; set; } = new();
    public TelemeteryConfiguration Telemetry { get; set; } = new TelemeteryConfiguration();
    public List<CustomDisplayField> CaseDisplayCustomFields { get; set; } = new();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }

    // ------- Configuration Objects -------

    public class TelemeteryConfiguration
    {
        public string CollectorUrl { get; set; } = string.Empty;
        public string AzureConnectionString { get; set; } = string.Empty;
        public bool LogToConsole { get; set; }

    }


    public class EdtClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SubmittingAgencyGroup { get; set; } = string.Empty;
        public int SearchFieldId { get; set; }
    }
    public class ConnectionStringConfiguration
    {
        public string CaseManagementDataStore { get; set; } = string.Empty;
    }

    public class KeycloakConfiguration
    {
        public string RealmUrl { get; set; } = string.Empty;
        public string WellKnownConfig => KeycloakUrls.WellKnownConfig(this.RealmUrl);
        public string TokenUrl => KeycloakUrls.Token(this.RealmUrl);
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

    public class CustomDisplayField
    {
        public int Id { get; set; }
        public bool Display { get; set; }

        public int RelatedId { get; set; }
        public bool RelatedValueEmpty { get; set; }
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
        public string BootstrapServers { get; set; } = string.Empty;
        public string CaseAccessRequestTopicName { get; set; } = string.Empty;
        public string CaseAccessResponseTopicName { get; set; } = string.Empty;
        public string CourtLocationAccessRequestTopic { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;

        public string AckTopicName { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "caseaccess-consumer-group";
        public string RetryConsumerGroupId { get; set; } = "caseaccess-retry-consumer-group";



    }


    public class MailServerConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}

