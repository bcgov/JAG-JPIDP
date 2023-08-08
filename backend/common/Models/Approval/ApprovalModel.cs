namespace Common.Models.Approval;

using NodaTime;

public class ApprovalModel
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Instant? Approved { get; set; }
    public Instant? Deleted { get; set; }
    public Instant? Requested { get; set; }
    public IEnumerable<AccessRequest> Requests { get; set; } = Enumerable.Empty<AccessRequest>();
}

public class ApprovalHistory
{
    public string DecisionNote { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
    public int ApprovalRequestId { get; set; }
    public Instant? Deleted { get; set; }
}

public class AccessRequest
{
    public int AccessRequestId { get; set; }
    public string RequestType { get; set; }
    public ApprovalStatus Status { get; set; }
    public IEnumerable<ApprovalHistory> History { get; set; } = Enumerable.Empty<ApprovalHistory>();

}


public enum ApprovalStatus
{
    APPROVED,
    DENIED,
    PENDING,
    DEFERRED,
    OTHER

}
