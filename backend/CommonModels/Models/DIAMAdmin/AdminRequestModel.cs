namespace CommonModels.Models.DIAMAdmin;

/// <summary>
/// Represents a request for a service to take an admin action
/// For instance this could represent a request to remove a users access to a service
/// </summary>
public class AdminRequestModel
{
    public required string TargetKafkaInstance { get; set; }
    public required string TargetEnvironment { get; set; }
    public required string RequestType { get; set; }
    public required string Requestor { get; set; }
    public string? RequestorIPAddress { get; set; }
    public Dictionary<string, string> RequestData { get; set; } = [];
    public DateTime RequestDateTime { get; set; }

}

