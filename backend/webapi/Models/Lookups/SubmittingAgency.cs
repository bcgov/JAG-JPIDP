namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum SubmittingAgencyCode
{
    VICPD = 0,
    DELTAPD = 1,
    SAANICHPD = 2,
}

[Table("SubmittingAgencyLookup")]
public class SubmittingAgency
{
    [Key]
    public SubmittingAgencyCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SubmittingAgencyDataGenerator : ILookupDataGenerator<SubmittingAgency>
{
    public IEnumerable<SubmittingAgency> Generate() => new[]
    {
        new SubmittingAgency{Code = SubmittingAgencyCode.VICPD, Name = "Victoria Police Department"},
        new SubmittingAgency{Code = SubmittingAgencyCode.DELTAPD, Name = "Delta Police Department"},
        new SubmittingAgency{Code = SubmittingAgencyCode.SAANICHPD, Name = "Saanich Police Department"}
    };
}
