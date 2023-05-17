namespace Pidp.Features.Admin.IdentityProviders;

public class IdentityProviderModel
{
    public string Alias { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();
    public string InternalId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;

}
