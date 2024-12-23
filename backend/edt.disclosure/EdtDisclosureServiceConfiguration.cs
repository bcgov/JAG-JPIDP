namespace edt.disclosure;
using edt.disclosure.Infrastructure.Auth;

public class EdtDisclosureServiceConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public int DisclosureClientTimeoutSeconds { get; set; } = 360;
    public ConnectionStringConfiguration ConnectionStrings { get; set; } = new();
    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public EdtDisclosureClientConfiguration EdtClient { get; set; } = new();

    public KeycloakConfiguration Keycloak { get; set; } = new();
    public List<CustomDisplayField> CaseDisplayCustomFields { get; set; } = new();

    public SchemaRegistryConfiguration SchemaRegistry { get; set; } = new();
    public TelemeteryConfiguration Telemetry { get; set; } = new TelemeteryConfiguration();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    // this is the ID know to core for disclosure (e.g. in disclosure it might be 1 but in core it may be 2 (as core is already 1))
    public string EdtCoreDisclosureInstanceId { get; set; } = "2";
    public string EdtInstanceId { get; set; } = "1";
    public string EdtOrgUnitId { get; set; } = "1";

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


    public class EdtDisclosureClientConfiguration
    {

        public bool CreateCourtLocations { get; set; }
        public int CourtLocationTemplateId { get; set; }
        public int DefenceFolioTemplateId { get; set; } = -1;
        public int OutOfCustodyTemplateId { get; set; } = -1;
        public string ApiKey { get; set; } = string.Empty;
        public string CounselGroups { get; set; } = "Counsel";
        public string CourtLocationGroup { get; set; } = string.Empty;
        public string CourtLocationKeyPrefix { get; set; } = "CH-";
        public string DefenceCaseGroups { get; set; } = string.Empty;
        public string DefenceFolioTemplateName { get; set; } = string.Empty;
        public string OutOfCustodyCaseGroups { get; set; } = string.Empty;
        public string OutOfCustodyGroups { get; set; } = string.Empty;
        public string OutOfCustodyOrgName { set; get; } = "Public";
        public string OutOfCustodyOrgType { set; get; } = "Out-of-custody";
        public string OutOfCustodyTemplateName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool CreateUserFolios { get; set; } = true;
    }

    public class CustomDisplayField
    {
        public int Id { get; set; }
        public bool Display { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RelatedId { get; set; }
        public bool RelatedValueEmpty { get; set; }
    }
    public class ConnectionStringConfiguration
    {
        public string DisclosureDataStore { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
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


    public class KafkaClusterConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string BootstrapServers { get; set; } = string.Empty;
        public string CaseAccessRequestTopicName { get; set; } = string.Empty;
        public string CaseAccessResponseTopicName { get; set; } = string.Empty;
        public string CourtLocationAccessRequestTopic { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string CreateDefenceUserTopic { get; set; } = string.Empty;
        public string CreatePublicUserTopic { get; set; } = string.Empty;
        public string AckTopicName { get; set; } = string.Empty;
        public string UserModificationTopicName { get; set; } = string.Empty;

        public string NotificationTopic { get; set; } = string.Empty;
        public string ProcessResponseTopic { get; set; } = string.Empty;
        public string UserMergeEventTopic { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "disclosure-consumer-group";
        public string RetryConsumerGroupId { get; set; } = "disclosure-retry-consumer-group";
        public string CoreFolioCreationNotificationTopic { get; set; } = string.Empty;
        public string HostnameVerification { get; set; } = "Https";


    }

}

