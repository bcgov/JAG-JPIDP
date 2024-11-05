namespace Common.Exceptions;
using System;


public class DIAMUserProvisioningException : Exception
{
    public DIAMUserProvisioningException(string? message) : base(message)
    {
    }

    public DIAMUserProvisioningException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
