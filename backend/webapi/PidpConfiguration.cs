namespace Pidp;

using Common.Models.EDT;

public class PidpConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public string ApplicationUrl { get; set; } = string.Empty;
    public string CorrectionsIDP { get; set; } = "siteminder";
    public int AUFToolsCaseId { get; set; }
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
    public JUSTINClaimClientConfiguration JustinClaimClient { get; set; } = new JUSTINClaimClientConfiguration();

    public EdtClientConfiguration EdtClient { get; set; } = new EdtClientConfiguration();
    public EdtCaseManagementClientConfiguration EdtCaseManagementClient { get; set; } = new EdtCaseManagementClientConfiguration();
    public EdtClientConfiguration EdtDisclosureClient { get; set; } = new EdtClientConfiguration();
    public EnvironmentConfiguration EnvironmentConfig { get; set; } = new EnvironmentConfiguration();
    public List<PersonLookupType> EdtPersonLookups { get; set; } = [];
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    public CronConfig CourtAccess { get; set; } = new();
    public SanityCronConfig SanityCheck { get; set; } = new SanityCronConfig();
    public VerifiableCredentialsConfiguration VerifiableCredentials { get; set; } = new VerifiableCredentialsConfiguration();
    public TelemeteryConfiguration Telemetry { get; set; } = new TelemeteryConfiguration();
    public bool AllowUserPassTestAccounts { get; set; }

    // ------- Configuration Objects -------

    public class AddressAutocompleteClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    public class BackGroundServicesConfiguration
    {
        public DecomissionCaseAccessService DecomissionCaseAccessService { get; set; } = new DecomissionCaseAccessService();
        public SyncCaseAccessService SyncCaseAccessService { get; set; } = new SyncCaseAccessService();

    }

    public class PersonLookupType
    {
        public LookupType Type { get; set; }
        public string Name { get; set; }

    }

    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }

    public class DecomissionCaseAccessService
    {
        //  public int PeriodicTimer { get; set; }
        public int GracePeriod { get; set; }
        public string PollCron { get; set; } = "0 * * * * ?";
    }

    public class SyncCaseAccessService
    {
        //  public int PeriodicTimer { get; set; }
        public int GracePeriod { get; set; }
        public string PollCron { get; set; } = "0 * * * * ?";
    }


    public class ConnectionStringConfiguration
    {
        public string PidpDatabase { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public string EfHistorySchema { get; set; } = "public";
        public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
    }

    public class VerifiableCredentialsConfiguration
    {
        public string PresentedRequestId { get; set; } = string.Empty;
        public string RequiredStatusCode { get; set; } = string.Empty;
    }

    public class TelemeteryConfiguration
    {
        public string CollectorUrl { get; set; } = string.Empty;
        public string AzureConnectionString { get; set; } = string.Empty;
        public bool LogToConsole { get; set; }

    }

    public class SanityCronConfig
    {
        public string PollCron { get; set; } = "0 * * * * ?";
        public int RepublishDelayMinutes { get; set; } = 5;
        public int FailureDelayMinutes { get; set; } = 15;

    }

    public class CronConfig
    {
        public string PollCron { get; set; } = "0 * * * * ?";
    }

    public class ChesClientConfiguration
    {
        public bool Enabled { get; set; }
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
    }

    public class JUSTINClaimClientConfiguration
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
        public string AdministrationUrl { get; set; } = string.Empty;
        public string AdministrationClientId { get; set; } = string.Empty;
        public string AdministrationClientSecret { get; set; } = string.Empty;
        public string HcimClientId { get; set; } = string.Empty;
        public string BirthdateField { get; set; } = "birthdate";
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
        public string ProbeRequestTopicName { get; set; } = string.Empty;
        public string PersonCreationTopic { get; set; } = string.Empty;
        public string JamUserProvisioningTopic { get; set; } = string.Empty;
        public string DisclosureDefenceUserCreationTopic { get; set; } = string.Empty;
        public string DisclosurePublicUserCreationTopic { get; set; } = string.Empty;
        public string DisclosureUserModificationTopic { get; set; } = string.Empty;
        public string ApprovalCreationTopic { get; set; } = string.Empty;
        public string ProcessResponseTopic { get; set; } = string.Empty;
        public string UserAccountChangeTopicName { get; set; } = string.Empty;
        public string ParticipantCSNumberMappingTopic { get; set; } = string.Empty;
        public string NotificationTopicName { get; set; } = string.Empty;
        public string UserAccountChangeNotificationTopicName { get; set; } = string.Empty;
        public string ParticipantMergeResponseTopic { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string CourtLocationAccessRequestTopic { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";
        public string ConsumerGroupId { get; set; } = "dems-notification-ack";
        public string KafkaAdminClientId { get; set; } = string.Empty;
        public string KafkaAdminClientSecret { get; set; } = string.Empty;
        public string HostnameVerification { get; set; } = "Https";
        public string DIAMAdminIncomingTopic { get; set; } = string.Empty;
        public string DIAMAdminOutgoingTopic { get; set; } = string.Empty;

    }
    public class JumClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string RealmUrl { get; set; } = string.Empty;
        public string AdministrationUrl { get; set; } = string.Empty;
        public string AdministrationClientId { get; set; } = string.Empty;
        public string AdministrationClientSecret { get; set; } = string.Empty;
    }
    public class MailServerConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class EnvironmentConfiguration
    {
        public string SupportEmail { get; set; } = "jpsprovideridentityportal@gov.bc.ca";
        public string Environment { get; set; } = string.Empty;
    }

    public class EdtClientConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string RealmUrl { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public int MaxClientValidations { get; set; } = 5;
        public string DateOfBirthField { get; set; } = "Date of Birth";
        public string OneTimeCode { get; set; } = "OTC";

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
