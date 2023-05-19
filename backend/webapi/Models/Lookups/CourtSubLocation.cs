namespace Pidp.Models.Lookups
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("CourtSubLocation")]
    public class CourtSubLocation
    {
        [Key]
        public int CourtSubLocationId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;

        public CourtLocation CourtLocation { get; set; } = new CourtLocation();

    }
}
