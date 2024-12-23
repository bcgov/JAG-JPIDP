namespace CommonModels.Models.DIAMAdmin;

/// <summary>
/// Represents a request for a service to take an admin action
/// For instance this could represent a request to remove a users access to a service
/// </summary>
public class AdminRequestModel
{
    public string? TargetKafkaInstance { get; set; }
    public required string TargetEnvironment { get; set; }
    public required AdminCommandSet RequestType { get; set; }
    public required string Requestor { get; set; }
    public string? RequestorIPAddress { get; set; }
    public Dictionary<string, string> RequestData { get; set; } = [];
    public DateTime RequestDateTime { get; set; }

}

public class AdminRequestKey
{
    public required string Key { get; set; }
    public List<string> TargetServices { get; set; } = [];
}

