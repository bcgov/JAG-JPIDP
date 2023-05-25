namespace Pidp.Models;

using NodaTime;

public class SubmittingAgencyModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IdpHint { get; set; } = string.Empty;
    public Instant? ClientCertExpiry { get; set; }
    public int? LevelOfAssurance { get; set; }
    public bool HasRealm { get; set; }
    public bool HasIdentityProvider { get; set; }
    public bool HasIdentityProviderLink { get; set; }
    public List<string> Warnings { get; set; } = new List<string>();
}
