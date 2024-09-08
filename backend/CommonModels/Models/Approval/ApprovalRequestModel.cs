namespace Common.Models.Approval;
public class ApprovalRequestModel
{
    public List<ApprovalAccessRequest> AccessRequests { get; set; } = [];
    public List<ApprovalRequestReason> Reasons { get; set; } = [];
    public string RequiredAccess { get; set; } = string.Empty;
    public DateTime? Created { get; set; }
    public List<PersonalIdentityModel> PersonalIdentities { get; set; } = [];
    public int NoOfApprovalsRequired { get; set; } = 1; // by default just a single approver required
    public string EMailAddress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
}

public class ApprovalRequestReason
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? Created { get; set; }


}

public class ApprovalAccessRequest
{
    public int AccessRequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = [];

}
