namespace ApprovalFlow.Exceptions;

using Common.Models.Approval;

public class ApprovalResponseException : Exception
{
    public ApprovalResponseException(string? message) : base(message)
    {
    }


}
