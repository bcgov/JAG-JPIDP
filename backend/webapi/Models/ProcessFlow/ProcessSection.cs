namespace Pidp.Models.ProcessFlow;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pidp.Models.Lookups;

[Table(nameof(ProcessSection))]
public class ProcessSection : BaseAuditable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}


public class ProcessSectionDataGenerator : ILookupDataGenerator<ProcessSection>
{
    public IEnumerable<ProcessSection> Generate() => new[]
    {
        new ProcessSection { Name = "organizationDetails", Id=-1 },
        new ProcessSection { Name = "demographics", Id=-2 },
        new ProcessSection { Name = "driverFitness", Id=-3 },
        new ProcessSection { Name = "digitalEvidence", Id=-4 },
        new ProcessSection { Name = "digitalEvidenceCaseManagement", Id=-5 },
        new ProcessSection { Name = "digitalEvidenceCounsel", Id=-6 },
        new ProcessSection { Name = "submittingAgencyCaseManagement", Id=-7 },
        new ProcessSection { Name = "uci", Id=-8 },
        new ProcessSection { Name = "administratorInfo", Id=-9 },
        new ProcessSection { Name = "admin", Id=-10 },


    };
}
