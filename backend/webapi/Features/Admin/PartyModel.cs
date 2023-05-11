namespace Pidp.Features.Admin;

using NodaTime;

public class PartyModel
{
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public string Email { get; set; }
    public bool Enabled { get; set; }
    public string IdentityProvider { get; set; }
    public decimal ParticipantId { get; set; }
    public Instant? Created { get; set; }
    public Guid KeycloakUserId { get; set; }
    public Dictionary<string, string> UserIssues { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, SystemUserModel> SystemsAccess { get; set; } = new Dictionary<string, SystemUserModel>();


}

public class SystemUserModel
{
    public string System { get; set; }
    public string Username { get; set; }
    public bool Enabled { get; set; }
    public string AccountType { get; set; }
    public string Key { get; set; }
    public bool IsAdmin { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public List<string> Regions { get; set; } = new List<string>();

}
