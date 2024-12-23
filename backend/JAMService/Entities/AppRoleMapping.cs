namespace JAMService.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a mapping of roles between two applications - e.g. JUSTIN and Keycloak
/// </summary>
public class AppRoleMapping
{
    [Key]
    public int Id { get; set; }
    public string? Description { get; set; } = string.Empty;
    public Application? Application { get; set; }
    public int ApplicationId { get; set; }
    public bool ExactSourceRoleMatch { get; set; } = true;
    public List<string> TargetRoles { get; set; } = [];
    public List<string> SourceRoles { get; set; } = [];
    public bool IsRealmGroup { get; set; }

}
