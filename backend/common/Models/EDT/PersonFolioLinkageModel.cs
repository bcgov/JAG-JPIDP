namespace Common.Models.EDT;

public class PersonFolioLinkageModel
{
    public string PersonKey { get; set; } = string.Empty;
    public string DisclosureCaseIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PersonType { get; set; } = string.Empty;
    public int AccessRequestId { get; set; }


}
