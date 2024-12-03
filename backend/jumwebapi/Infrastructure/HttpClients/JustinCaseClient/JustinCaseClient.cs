namespace jumwebapi.Infrastructure.HttpClients.JustinCases;

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using CommonModels.Models.JUSTIN;
using global::Common.Exceptions;
using global::Common.Models.JUSTIN;

/// <summary>
/// Client for getting case status
/// </summary>
public class JustinCaseClient(HttpClient httpClient, ILogger<JustinCaseClient> logger) : BaseClient(httpClient, logger), IJustinCaseClient
{
    public async Task<CaseStatusWrapper> GetCaseStatus([Required] string encodedCaseId, string accessToken)
    {
        var caseId = WebUtility.UrlDecode(encodedCaseId);
        if (!caseId.Contains(':', StringComparison.CurrentCulture))
        {
            this.Logger.LogInvalidCaseStatusRequest(caseId);
            throw new DIAMGeneralException($"[{caseId}] is invalid for case lookup");
        }
        var fileSegments = caseId.Split(':');
        if (fileSegments.Length != 2)
        {
            this.Logger.LogInvalidCaseStatusRequest(caseId);
            throw new DIAMGeneralException($"[{caseId}] is invalid for case lookup");
        }

        var result = await this.GetAsync<CaseStatusResponseModel>($"agencyFileStatus?agency_identifier_cd={WebUtility.UrlEncode(fileSegments[0].Trim())}&agency_file_number={WebUtility.UrlEncode(fileSegments[1].Trim())}", accessToken);

        if (result.IsSuccess && result.Value != null)
        {
            this.Logger.LogCaseStatusFound(caseId, result.Value.AgencyFileStatus);
            var caseStatus = CaseStatus.GetByValue(result.Value.AgencyFileStatus);

            var caseStatusWrapper = new CaseStatusWrapper(value: caseStatus.Value, demsCandidate: caseStatus.DemsCandidate, description: caseStatus.Description)
            {
                RccId = string.IsNullOrEmpty(result.Value.RccId) ? "" : result.Value.RccId
            };

            return caseStatusWrapper = caseStatus.Equals(CaseStatus.NotFound) ? null : caseStatusWrapper;
        }
        else
        {
            if (result.Status == DomainResults.Common.DomainOperationStatus.NotFound)
            {
                this.Logger.LogCaseNotFoundForLookup(caseId);
            }
            else
            {
                this.Logger.LogCaseStatusRestError(caseId, string.Join(",", result.Errors));
            }
            return null;
        }


    }
}

public static partial class JustinCaseClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Case {caseId} status is {status}.")]
    public static partial void LogCaseStatusFound(this ILogger logger, string caseId, string status);
    [LoggerMessage(2, LogLevel.Error, "Invalid case status request {caseId}")]
    public static partial void LogInvalidCaseStatusRequest(this ILogger logger, string caseId);
    [LoggerMessage(3, LogLevel.Warning, "Case lookup failure - invalid caseId {caseId}")]
    public static partial void LogCaseNotFoundForLookup(this ILogger logger, string caseId);
    [LoggerMessage(4, LogLevel.Error, "Case {caseId} lookup failure - rest error {error}")]
    public static partial void LogCaseStatusRestError(this ILogger logger, string caseId, string error);
}
