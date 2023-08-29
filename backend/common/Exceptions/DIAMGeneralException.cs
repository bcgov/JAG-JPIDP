namespace Common.Exceptions;
using System;
using System.Runtime.Serialization;

public class DIAMGeneralException : Exception
{
    public DIAMGeneralException()
    {
    }

    public DIAMGeneralException(string? message) : base(message)
    {
    }

    public DIAMGeneralException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected DIAMGeneralException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
