namespace CommonModels.Models.JUSTIN;

using Common.Models.JUSTIN;
using NodaTime;



/// <summary>
/// Represents two JUSTIN records merged together
/// </summary>
public class ParticipantMergeDetailModel
{
    public int MergeId { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public Instant CreatedOn { get; set; } = Instant.FromDateTimeUtc(DateTime.UtcNow);
    public ParticipantDetail? SourceParticipant { get; set; }
    public ParticipantDetail? TargetParticipant { get; set; }

}

public enum ParticipantType
{
    ACCUSED,
    LEGALCOUNSEL,
    BCPSUSER
}

