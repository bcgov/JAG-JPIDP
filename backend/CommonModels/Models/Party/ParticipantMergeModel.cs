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


    /// <summary>
    /// Get all matching values across primary and any secondary participants
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public IEnumerable<string> GetAllMatchingFieldValues(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
        }

        var distinctValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (this.PrimaryParticipant != null && this.PrimaryParticipant.ParticipantInfo.TryGetValue(fieldName, out var primaryValue))
        {
            distinctValues.Add(primaryValue);
        }

        foreach (var participant in this.SourceParticipants)
        {
            if (participant.ParticipantInfo.TryGetValue(fieldName, out var value))
            {
                distinctValues.Add(value);
            }
        }

        return distinctValues;
    }

    /// <summary>
    /// Get all participants
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ParticipantMergeModel> GetAllParticipants()
    {
        var response = new List<ParticipantMergeModel>();
        if (this.PrimaryParticipant != null)
        {
            response.Add(this.PrimaryParticipant);
        }

        response.AddRange(this.SourceParticipants);

        return response;
    }
}
