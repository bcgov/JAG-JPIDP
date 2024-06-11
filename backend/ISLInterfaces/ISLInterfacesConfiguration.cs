namespace ISLInterfaces;

using ISLInterfaces.Infrastructure.Auth;

public class ISLInterfacesConfiguration
{
    public static bool IsProduction() => EnvironmentName == Environments.Production;
    public static bool IsDevelopment() => EnvironmentName == Environments.Development;
    private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    public KeycloakConfiguration Keycloak { get; set; } = new();
    public const string KeycloakConfig = "Keycloak";
    public const string DatabaseConnectionInfoConfig = "DatabaseConnectionInfo";

    public ConnectionStringConfiguration DatabaseConnectionInfo { get; set; } = new();
}

public class ConnectionStringConfiguration
{
    public string CaseManagementDataStore { get; set; } = string.Empty;
    public string Schema { get; set; } = "public";
    public string EfHistorySchema { get; set; } = "public";
    public string EfHistoryTable { get; set; } = "__EFMigrationsHistory";
}

public class KeycloakConfiguration
{
    public string RealmUrl { get; set; } = string.Empty;
    public string WellKnownConfig => KeycloakUrls.WellKnownConfig(this.RealmUrl);
    public string TokenUrl => KeycloakUrls.Token(this.RealmUrl);
}