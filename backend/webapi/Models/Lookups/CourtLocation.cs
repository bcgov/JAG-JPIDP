namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("CourtLocation")]
public class CourtLocation
{
    [Key]
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public bool Staffed { get; set; } = true;

    public ICollection<CourtSubLocation> CourtSubLocations { get; set; } = new List<CourtSubLocation>();


    public class CourtLocationDataGenerator : ILookupDataGenerator<CourtLocation>
    {

        public IEnumerable<CourtLocation> Generate() => [];

    }
}
