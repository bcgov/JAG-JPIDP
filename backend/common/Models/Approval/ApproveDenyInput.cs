namespace Common.Models.Approval;
using System;
using MediatR;

/// <summary>
/// Used to approve/deny a request
/// </summary>
public class ApproveDenyInput : IRequest<ApprovalModel>
{
    public int ApprovalRequestId { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public string DecisionNotes { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string? ApproverUserId { get; set; }

}
