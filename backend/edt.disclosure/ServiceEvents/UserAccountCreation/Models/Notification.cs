namespace edt.disclosure.ServiceEvents.UserAccountCreation.Models;

using System.ComponentModel.DataAnnotations;

public class Notification
{
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string? To { get; set; }
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string? From { get; set; }
    public string? Subject { get; set; }

    public string DomainEvent { get; set; } = string.Empty;

    public Dictionary<string, string> EventData { get; set; } = new Dictionary<string, string>();

}
