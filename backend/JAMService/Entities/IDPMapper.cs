namespace JAMService.Entities;

using System.ComponentModel.DataAnnotations;

public class IDPMapper
{
    [Key]
    public int Id { get; set; }

    public string SourceRealm { get; set; } = string.Empty;
    public string SourceIdp { get; set; } = string.Empty;
    public string TargetRealm { get; set; } = string.Empty;
    public string TargetIdp { get; set; } = string.Empty;
}
