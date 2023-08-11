namespace Common.Models.Approval;

using NodaTime;

public class ApprovalModel
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Instant? Approved { get; set; }
    public Instant? Deleted { get; set; }
    public Instant? Created { get; set; }
    public Instant? Modified { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
    public IEnumerable<RequestModel> Requests { get; set; } = Enumerable.Empty<RequestModel>();
}

public class ApprovalHistoryModel
{
    public string DecisionNote { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
    public int ApprovalRequestId { get; set; }
    public Instant? Deleted { get; set; }
}

public class RequestModel
{
    public int RequestId { get; set; }
    public string RequestType { get; set; }
    public string ApprovalType { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.PENDING;
    public IEnumerable<ApprovalHistoryModel> History { get; set; } = Enumerable.Empty<ApprovalHistoryModel>();

}


public enum ApprovalStatus
{
    APPROVED,
    DENIED,
    PENDING,
    DEFERRED,
    OTHER

}
