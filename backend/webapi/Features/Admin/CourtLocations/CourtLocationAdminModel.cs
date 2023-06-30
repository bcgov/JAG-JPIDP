namespace Pidp.Features.Admin.CourtLocations;

using Pidp.Models.Lookups;

public class CourtLocationAdminModel : CourtLocation
{
    public int UserCount { get; set; }
    public string FolioId { get; set; }

}
