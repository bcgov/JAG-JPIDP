namespace CommonModels.Models.EDT;


/// <summary>
/// Used to tell EDT Disclosure when a user should be added to a Folio as a result of a merge event
/// </summary>
public class EdtDisclosureUserMergeUpdateModel
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? UserName { get; set; }
    public int SourceDisclosureCaseId { get; set; }
    public int TargetDisclosureCaseId { get; set; }
}
