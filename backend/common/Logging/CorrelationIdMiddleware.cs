namespace Common.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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
            logger.LogCorrelationRequest(correlationId, context.Request.GetDisplayUrl());
            await next(context);
        }
    }
}

public static partial class CommonLoggingExtensions
{
    //--------------------------------------------------------------------------------
    // Http Logging
    //--------------------------------------------------------------------------------
    [LoggerMessage(1, LogLevel.Trace, "CorrelationId: {correlationId} {requestUrl}")]
    public static partial void LogCorrelationRequest(this ILogger logger, string correlationId, string requestUrl);
}
