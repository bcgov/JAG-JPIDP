namespace ISLInterfaces;

public class SerilogHandler : DelegatingHandler
{
    private Serilog.ILogger _logger;

    public SerilogHandler(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        _logger.Debug("Request completed: {route} {method} {code} {headers}", request.RequestUri.Host, request.Method, response.StatusCode, request.Headers);
        return response;
    }
}
