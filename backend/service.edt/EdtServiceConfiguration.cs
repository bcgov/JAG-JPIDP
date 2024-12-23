namespace edt.service;

using System.Text.Json;

public class EdtServiceConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public AddressAutocompleteClientConfiguration AddressAutocompleteClient { get; set; } = new();
    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public ChesClientConfiguration ChesClient { get; set; } = new();
    public JustinParticipantClientConfiguration JustinParticipantClient { get; set; } = new();
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public KeycloakConfiguration Keycloak { get; set; } = new();
    public MailServerConfiguration MailServer { get; set; } = new();
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
    public EdtClientConfiguration EdtClient { get; set; } = new();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    public BackgroundServiceConfig FolioLinkageBackgroundService { get; set; } = new BackgroundServiceConfig();
    public SchemaRegistryConfiguration SchemaRegistry { get; set; } = new();
    public TelemeteryConfiguration Telemetry { get; set; } = new TelemeteryConfiguration();
    public ParticipantMergeLookup ParticipantMergeLookupConfig { get; set; } = new ParticipantMergeLookup();


    // ------- Configuration Objects -------

    public class TelemeteryConfiguration
    {
        public string CollectorUrl { get; set; } = string.Empty;
        public string AzureConnectionString { get; set; } = string.Empty;
        public bool LogToConsole { get; set; }

    }

    public class ParticipantMergeLookup
    {
        public string MergedParticipantField { get; set; } = "MergedParticipantKeys";
        public char MergedParticipantDelimiter { get; set; } = ',';
        public string[] AdditionalLookupFields { get; set; } = ["OTC"];
        public string DisclosurePortalCaseField { get; set; } = "DisclosurePortalCase";
        public string DateOfBirthField { get; set; } = "Date of birth";
        public string DateOfBirthFormat { get; set; } = "yyyy-MM-dd";
        public string[] Identifiers { get; set; } = ["DisclosurePortalCase"];
    }

    public class BackgroundServiceConfig
    {
        public int PollSeconds { get; set; } = 120; // default to every 2 minutes
        public int MaxRetriesForLinking { get; set; } = 10;
        public string PollCron { get; set; } = "0/30 * * ? * * *";

    }

    public class AddressAutocompleteClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    public class EdtClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string AdditionalBCPSGroups { get; set; } = string.Empty;
        public string DefenceParticipantAdditionalId { get; set; } = "PPID";
        public int SearchFieldId { get; set; }
        public string TombStoneEmailDomain { get; set; } = string.Empty;
    }
    public class ConnectionStringConfiguration
    {
        public string EdtDataStore { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
    }


    public class RetryTopicModel
    {
        public int RetryCount { get; set; }
        public int DelayMinutes { get; set; }
        public bool NotifyUser { get; set; }
        public bool NotifyOnEachRetry { get; set; }
        public int Order { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public override string ToString() => JsonSerializer.Serialize(this);

    }

    public class SchemaRegistryConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

    }

    public class RetryPolicyConfiguration
    {
        public string? DeadLetterTopic { get; set; }
        public List<RetryTopicModel> RetryTopics { get; set; } = new List<RetryTopicModel>();
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
        public string ConsumerTopicName { get; set; } = string.Empty;
        public string ProducerTopicName { get; set; } = string.Empty;
        public string AckTopicName { get; set; } = string.Empty;
        public string IncomingUserChangeTopic { get; set; } = string.Empty;
        public string UserModificationTopicName { get; set; } = string.Empty;
        public string PersonCreationTopic { get; set; } = string.Empty;
        public string ProcessResponseTopic { get; set; } = string.Empty;
        public string UserCreationTopicName { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "accessrequest-consumer-group";
        public string RetryConsumerGroupId { get; set; } = "accessrequest-retry-consumer-group";
        public string CoreFolioCreationNotificationTopic { get; set; } = string.Empty;
        public string HostnameVerification { get; set; } = "Https";

    }


    public class JustinParticipantClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
    }


    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }

    public class KeycloakConfiguration
    {
        public string RealmUrl { get; set; } = string.Empty;
        //public string TokenUrl => KeycloakUrls.Token(this.RealmUrl);
        //public string AdministrationUrl { get; set; } = string.Empty;
        //public string AdministrationClientId { get; set; } = string.Empty;
        //public string AdministrationClientSecret { get; set; } = string.Empty;
        //public string HcimClientId { get; set; } = string.Empty;
    }
    public class MailServerConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public int Port { get; set; }

    }
}
