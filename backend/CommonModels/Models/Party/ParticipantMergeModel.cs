namespace CommonModels.Models.Party;
/// <summary>
/// Represents a set of participants that have been merged together
/// </summary>
public class ParticipantMergeModel
{
    public string ParticipantId { get; set; } = string.Empty;
    // used to store things like info pulled from EDT or JUSTIN
    public Dictionary<string, string> ParticipantInfo { get; set; } = [];
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public int? EdtID { get; set; }
}

public class ParticipantMergeListingModel
{
    public ParticipantMergeModel? PrimaryParticipant { get; set; }
    public List<ParticipantMergeModel> SourceParticipants { get; set; } = [];

}
