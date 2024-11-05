namespace JAMService.Infrastructure.HttpClients.JustinParticipant;

using CommonModels.Models.JUSTIN;

public class JustinParticipantRoleClient(HttpClient httpClient, ILogger<JustinParticipantRoleClient> logger) : BaseClient(httpClient, logger), IJustinParticipantRoleClient
{
    public async Task<DbRoles> GetParticipantRolesByApplicationNameAndParticipantId(string application, double participantId)
    {
        var result = await this.GetAsync<DbRoles>($"?app={application}&partId={participantId}");
        if (!result.IsSuccess)
        {
            this.Logger.LogJustinQueryFailure(string.Join(",", result.Errors));
            return new DbRoles();
        }

        return result.Value;

    }


    public async Task<DbRoles> GetParticipantRolesByApplicationNameAndUPN(string application, string UPN)
    {
        var result = await this.GetAsync<DbRoles>($"?app={application}&UPN={UPN}");
        if (!result.IsSuccess)
        {
            this.Logger.LogJustinQueryFailure(string.Join(",", result.Errors));
            return new DbRoles();
        }

        return result.Value;


    }
}
public static partial class JustinParticipantClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "No User found in JUM with Username = {username}.")]
    public static partial void LogNoUserFound(this ILogger logger, string username);
    [LoggerMessage(2, LogLevel.Warning, "No User found in JUM with PartId = {partId}.")]
    public static partial void LogNoUserWithPartIdFound(this ILogger logger, decimal partId);
    [LoggerMessage(3, LogLevel.Warning, "User found but disabled in JUM with Username = {username}.")]
    public static partial void LogDisabledUserFound(this ILogger logger, string username);
    [LoggerMessage(4, LogLevel.Warning, "User found but disabled in JUM with PartId = {partId}.")]
    public static partial void LogDisabledPartIdFound(this ILogger logger, decimal partId);
    [LoggerMessage(5, LogLevel.Error, "Justin user not found.")]
    public static partial void LogJustinUserNotFound(this ILogger logger);
    [LoggerMessage(6, LogLevel.Error, "Failed to query JUSTIN system [{errors}]")]
    public static partial void LogJustinQueryFailure(this ILogger logger, string errors);
}
