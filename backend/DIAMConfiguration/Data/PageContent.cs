namespace DIAMConfiguration.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;

[Table(nameof(PageContent))]

public class PageContent : BaseAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ContentKey { get; set; } = string.Empty;
    [Required]
    public string Resource { get; set; } = string.Empty;
    public string? Content { get; set; } = string.Empty;

    public IList<HostConfig> Hosts { get; } = new List<HostConfig>(); // Collection navigation containing dependents

}


