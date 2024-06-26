namespace MessagingAdapter;


public class MessagingAdapterConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public KafkaClusterConfiguration KafkaCluster { get; set; } = new();
    public EdtClientConfiguration EdtClient { get; set; } = new();
    public SplunkConfiguration SplunkConfig { get; set; } = new SplunkConfiguration();
    public string DataStoreConnection { get; set; } = string.Empty;

    // ------- Configuration Objects -------
    public class EdtClientConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }


    public class KafkaClusterConfiguration
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string ProducerTopicName { get; set; } = string.Empty;
        public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
        public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid";

    }



    public class SplunkConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public string CollectorToken { get; set; } = string.Empty;
    }


}
