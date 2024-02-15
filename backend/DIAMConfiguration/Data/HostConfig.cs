namespace DIAMConfiguration.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(HostConfig))]
public class HostConfig : BaseEntity
{

    [Required]
    public string Hostname { get; set; } = string.Empty;

    public IList<LoginConfig> HostLoginConfigs { get; set; } = new List<LoginConfig>();
    public IList<PageContent> PageContents { get; set; } = new List<PageContent>();

}
