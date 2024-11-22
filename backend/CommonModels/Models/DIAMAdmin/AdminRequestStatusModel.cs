namespace CommonModels.Models.DIAMAdmin;


public class AdminRequestStatusModel
{
    public int RequestId { get; set; }
    public string? Instance { get; set; }
    public string? KafkaInstance { get; set; }
    public required Guid MessageId { get; set; }
    public required string Requestor { get; set; }
    public required string RequestIPAddress { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string? Errors { get; set; }
    public required string RequestType { get; set; }
    public string? Status { get; set; }
    public string? RequestData { get; set; }
    public string? ResponseData { get; set; }


}
