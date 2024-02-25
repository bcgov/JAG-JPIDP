namespace DIAMConfiguration.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(LoginConfig))]
public class LoginConfig : BaseEntity
{

    [Required]
    public string Idp { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public LoginOptionType Type { get; set; }

    public int HostConfigId { get; set; }
    //public IList<HostConfig> Hosts { get; } = new List<HostConfig>(); // Collection navigation containing dependents

    public string? Notification { get; set; } = string.Empty;
    public string? FormControl { get; set; } = string.Empty;
    public string? FormList { get; set; } = string.Empty;

}

public enum LoginOptionType
{
    BUTTON,
    AUTOCOMPLETE
}
