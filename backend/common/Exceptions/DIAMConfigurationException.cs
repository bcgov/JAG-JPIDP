namespace Common.Exceptions;

using Prometheus;

public class DIAMConfigurationException : Exception
{
    private readonly Counter configExceptionCounter = Metrics.CreateCounter("diam_config_exception_total", "DIAM Config exception counter");


    public DIAMConfigurationException(string? message, Exception? innerException) : base(message, innerException) => this.configExceptionCounter.Inc(1);

    public DIAMConfigurationException(string? message) : base(message) => this.configExceptionCounter.Inc(1);
}

