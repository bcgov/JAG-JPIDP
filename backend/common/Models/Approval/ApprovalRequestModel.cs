namespace Common.Models.Approval;

using NodaTime;

public class ApprovalRequestModel
{
    public int AccessRequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public List<string>? Reasons { get; set; }
    public Instant Created { get; set; }

}
