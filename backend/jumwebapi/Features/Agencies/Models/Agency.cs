namespace jumwebapi.Features.Agencies.Models;

public class Agency
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AgencyCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    //public ICollection<JustinAgencyAssignment> AgencyAssignments { get; set; } = new List<JustinAgencyAssignment>();
}
