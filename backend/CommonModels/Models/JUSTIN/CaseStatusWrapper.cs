namespace CommonModels.Models.JUSTIN;

using Common.Models.JUSTIN;

public class CaseStatusWrapper(string value, bool demsCandidate, string description) : CaseStatus(value, demsCandidate, description)
{
    public string? RccId { get; set; } = string.Empty;
}
