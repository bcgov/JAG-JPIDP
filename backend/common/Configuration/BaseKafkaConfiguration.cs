namespace Common.Configuration;

using Microsoft.Extensions.Hosting;

public class BaseKafkafiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public string Url { get; set; } = string.Empty;
    public string BootstrapServers { get; set; } = string.Empty;
    public string SaslOauthbearerTokenEndpointUrl { get; set; } = string.Empty;
    public string SaslOauthbearerProducerClientId { get; set; } = string.Empty;
    public string SaslOauthbearerProducerClientSecret { get; set; } = string.Empty;
    public string SaslOauthbearerConsumerClientId { get; set; } = string.Empty;
    public string SaslOauthbearerConsumerClientSecret { get; set; } = string.Empty;
    public string SslCaLocation { get; set; } = string.Empty;
    public string SslCertificateLocation { get; set; } = string.Empty;
    public string SslKeyLocation { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid";

}

