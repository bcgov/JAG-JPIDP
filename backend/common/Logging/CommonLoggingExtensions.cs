namespace Common.Logging;
using System;
using System.Net;
using Microsoft.Extensions.Logging;

public static partial class CommonLoggingExtensions
{
    //--------------------------------------------------------------------------------
    // Http Logging
    //--------------------------------------------------------------------------------
    [LoggerMessage(1, LogLevel.Error, "Received non-success status code {statusCode} with message: {responseMessage}.")]
    public static partial void LogNonSuccessStatusCode(this ILogger logger, HttpStatusCode statusCode, string responseMessage);

    [LoggerMessage(2, LogLevel.Error, "Response content was null.")]
    public static partial void LogNullResponseContent(this ILogger logger);

    [LoggerMessage(3, LogLevel.Error, "Unhandled exception when calling the API.")]
    public static partial void LogBaseClientException(this ILogger logger, Exception e);


    //--------------------------------------------------------------------------------
    // Kafka Logging
    //--------------------------------------------------------------------------------
    [LoggerMessage(4, LogLevel.Information, "Message {msgId} sent to partition {partition}")]
    public static partial void LogKafkaMsgSent(this ILogger logger, string msgId, int partition);


    [LoggerMessage(5, LogLevel.Error, "Message {msgId} failed to send Status: {status}")]
    public static partial void LogKafkaMsgSendFailure(this ILogger logger, string msgId, string status);
}
