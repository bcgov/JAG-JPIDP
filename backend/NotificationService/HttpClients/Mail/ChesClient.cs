using DomainResults.Common;
using NotificationService.Exceptions;
using NotificationService.HttpClients;

namespace NotificationService.HttpClients.Mail;

public class ChesClient : BaseClient, IChesClient
{
    public ChesClient(HttpClient httpClient, ILogger<ChesClient> logger) : base(httpClient, logger) { }

    public async Task<EmailSuccessResponse?> SendAsync(Email email)
    {
        var result = await this.PostAsync<EmailSuccessResponse>("email", new ChesEmailRequestParams(email));
        if (!result.IsSuccess)
        {
            Serilog.Log.Error("CHES Email error {0} [{1}]", string.Join(",", result.Errors), email);
            throw new DeliveryException(string.Join(",", result.Errors));
        }

        return result.Value;
    }

    public async Task<string?> GetStatusAsync(Guid msgId)
    {
        var result = await this.GetWithQueryParamsAsync<IEnumerable<StatusResponse>>("status", new { msgId });
        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value.Single().Status;
    }

    public async Task<bool> HealthCheckAsync()
    {
        var result = await this.SendCoreAsync(HttpMethod.Get, "health", null, default);
        if (!result.IsSuccess)
        {
            Serilog.Log.Error("CHES Email service is not available {0}", string.Join(",", result.Errors));
        }
        return result.IsSuccess;
    }
}
