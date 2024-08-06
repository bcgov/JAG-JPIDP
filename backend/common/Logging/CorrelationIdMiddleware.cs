using Microsoft.AspNetCore.Http.Extensions;

namespace Common.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);
        using (LogContext.PushProperty(CorrelationIdHeader, correlationId))
        {
            logger.LogTrace($"CorrelationId: {correlationId} {context.Request.GetEncodedUrl}");
            await next(context);
        }
    }
}
