namespace Pidp.Models;

using NodaTime;

public class PublicDisclosureAccess
{
    public PublicDisclosureAccess() { }

    public string? KeyData { get; set; } = string.Empty;
    public Instant? Created { get; set; }
    public Instant? CompletedOn { get; set; }
    public string? RequestStatus { get; set; } = string.Empty;
}
