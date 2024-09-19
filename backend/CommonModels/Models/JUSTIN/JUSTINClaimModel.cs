namespace CommonModels.Models.JUSTIN;


public class JUSTINClaimModel
{
    public double PartId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Errors { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = [];
    public IEnumerable<string> AgencyAssignmnets { get; set; } = [];

}
