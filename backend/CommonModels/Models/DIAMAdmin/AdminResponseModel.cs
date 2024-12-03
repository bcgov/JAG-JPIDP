namespace CommonModels.Models.DIAMAdmin;

/// <summary>
/// When a service has picked up and processed an AdminRequest a response
/// should be generated that contains response data
/// </summary>
public class AdminResponseModel
{
    public required Guid RequestId { get; set; }
    public DateTime RequestProcessDateTime { get; set; }
    public required string Hostname { get; set; }
    public AdminCommandSet RequestType { get; set; }
    public string? Errors { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, string> ResponseData { get; set; } = [];

}
