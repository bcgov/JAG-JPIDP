namespace Common.Models.Approval;

using NodaTime;

public class ApprovalModel
{
    public int Id { get; set; }
    public List<ReasonModel> Reasons { get; set; } = [];
    public string RequiredAccess { get; set; } = string.Empty;
    public int NoOfApprovalsRequired { get; set; }
    public Instant? Approved { get; set; }
    public Instant? Completed { get; set; }
    public Instant? Deleted { get; set; }
    public Instant? Created { get; set; }
    public Instant? Modified { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
    public List<PersonalIdentityModel> PersonalIdentities { get; set; } = [];
    public IEnumerable<RequestModel> Requests { get; set; } = [];
}

public class ApprovalHistoryModel : AuditModel
{
    public string DecisionNote { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
    public int ApprovalRequestId { get; set; }
    public Instant? Deleted { get; set; }
}

public class ReasonModel : AuditModel
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ApprovalRequestId { get; set; }

}

public class RequestModel : AuditModel
{
    public int RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string ApprovalType { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.PENDING;
    public IEnumerable<ApprovalHistoryModel> History { get; set; } = [];

}

public class AuditModel
{
    public Instant? Created { get; set; }
    public Instant? Modified { get; set; }
}

public enum ApprovalStatus
{
    APPROVED,
    DENIED,
    PENDING,
    DEFERRED,
    OTHER

}
