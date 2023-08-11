namespace ApprovalFlow.Exceptions;

public class IncomingApprovalException : Exception
{
    public IncomingApprovalException(string? message) : base(message)
    {
    }
}
