namespace Common.Exceptions;
using System;
using System.Runtime.Serialization;
using Prometheus;

public class DIAMAuthException : Exception
{

    Counter prom_exception = Metrics.CreateCounter("diam_auth_exception_total", "DIAM Authorization exception counter");

    public DIAMAuthException() => this.prom_exception.Inc(1);


    public DIAMAuthException(string? message) : base(message)
    {
        this.prom_exception.Inc(1);
    }

    public DIAMAuthException(string? message, Exception? innerException) : base(message, innerException)
    {
        this.prom_exception.Inc(1);
    }

    protected DIAMAuthException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        this.prom_exception.Inc(1);
    }
}
