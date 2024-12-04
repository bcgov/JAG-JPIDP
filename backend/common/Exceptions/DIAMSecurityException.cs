namespace Common.Exceptions;
using System;

public class DIAMSecurityException : Exception
{

    public DIAMSecurityException(string? message) : base(message)
    {
    }

    public DIAMSecurityException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
