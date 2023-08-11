namespace Common.Models.Approval;

using NodaTime;

public class ApprovalRequestModel
{
    public List<ApprovalAccessRequest> AccessRequests { get; set; }
    public List<string>? Reasons { get; set; }
    public DateTime? Created { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
}

public class ApprovalAccessRequest
{
    public int AccessRequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public List<string>? Reasons { get; set; }

}
