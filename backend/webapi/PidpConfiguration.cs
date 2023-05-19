namespace Pidp;

using Pidp.Infrastructure.Auth;
using Serilog;

public class PidpConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public string ApplicationUrl { get; set; } = string.Empty;

    public AddressAutocompleteClientConfiguration AddressAutocompleteClient { get; set; } = new();
    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public ChesClientConfiguration ChesClient { get; set; } = new();
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public KeycloakConfiguration Keycloak { get; set; } = new();
    public LdapClientConfiguration LdapClient { get; set; } = new();
    public MailServerConfiguration MailServer { get; set; } = new();
    public BackGroundServicesConfiguration BackGroundServices { get; set; } = new();
    public PlrClientConfiguration PlrClient { get; set; } = new();
    public JumClientConfiguration JumClient { get; set; } = new();
    public EdtClientConfiguration EdtClient { get; set; } = new EdtClientConfiguration();
    public EdtCaseManagementClientConfiguration EdtCaseManagementClient { get; set; } = new EdtCaseManagementClientConfiguration();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    public CourtAccessConfiguration CourtAccess { get; set; } = new();  

    public TelemeteryConfiguration Telemetry { get; set; } = new TelemeteryConfiguration();


    // ------- Configuration Objects -------

    public class AddressAutocompleteClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    public class BackGroundServicesConfiguration
    {
        public DecomissionCaseAccessService DecomissionCaseAccessService { get; set; } = new DecomissionCaseAccessService();
    }

    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }

    public class DecomissionCaseAccessService
    {
        public int PeriodicTimer { get; set; }
        public int GracePeriod { get; set; }
    }
    public class ConnectionStringConfiguration
    {
        public string PidpDatabase { get; set; } = string.Empty;
    }

    public class TelemeteryConfiguration
    {
        public string CollectorUrl { get; set; } = string.Empty;
        public string AzureConnectionString { get; set; } = string.Empty;
        public bool LogToConsole { get; set; }

    }

    public class CourtAccessConfiguration
    {
        public int PollSeconds { get; set; } = 600;
    }

    public class ChesClientConfiguration
    {
        public bool Enabled { get; set; }
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
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

    public class LdapClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
    }
    public class KafkaClusterConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BootstrapServers { get; set; } = string.Empty;
        public string ConsumerTopicName { get; set; } = string.Empty;
        public string IncomingChangeEventTopic { get; set; } = string.Empty;
        public string ProducerTopicName { get; set; } = string.Empty;
        public string CaseAccessRequestTopicName { get; set; } = string.Empty;
        public string ProcessResponseTopic { get; set;} = string.Empty;
        public string UserAccountChangeTopicName { get; set; } = string.Empty;
        public string NotificationTopicName { get; set; } = string.Empty;
        public string UserAccountChangeNotificationTopicName { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string CourtLocationAccessRequestTopic { get; set;  } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "dems-notification-ack";

    }
    public class JumClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string RealmUrl { get; set; } = string.Empty;
        public string TokenUrl => KeycloakUrls.Token(this.RealmUrl);
        public string AdministrationUrl { get; set; } = string.Empty;
        public string AdministrationClientId { get; set; } = string.Empty;
        public string AdministrationClientSecret { get; set; } = string.Empty;
    }
    public class MailServerConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class EdtClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
    }

    public class EdtCaseManagementClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
    }

    public class PlrClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
    }
}
