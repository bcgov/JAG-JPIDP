namespace Common.Models.Approval;

using NodaTime;

public class ApprovalRequestModel
{
    public List<ApprovalAccessRequest> AccessRequests { get; set; }
    public List<string>? Reasons { get; set; }
    public string RequiredAccess { get; set; } = string.Empty;
    public DateTime? Created { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public int NoOfApprovalsRequired { get; set; } = 1; // by default just a single approver required
    public string EMailAddress { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
}

public class ApprovalAccessRequest
{
    public int AccessRequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public List<string>? Reasons { get; set; }

}
