namespace Pidp.Features.Admin.CourtLocations;

using Common.Models.EDT;
using Pidp.Models.Lookups;

public class CourtLocationAdminModel : CourtLocation
{
    public int UserCount { get; set; }
    public int EdtId { get; set; }
    public string Status { get; set; }
    public List<EdtField> EdtFields { get; set; } = new List<EdtField>();

    public string Key => "CH-" + this.Code;

}
