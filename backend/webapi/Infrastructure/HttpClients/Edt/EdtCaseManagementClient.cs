namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Net;
using System.Threading.Tasks;
using Pidp.Models;

public class EdtCaseManagementClient : BaseClient, IEdtCaseManagementClient
{

    public EdtCaseManagementClient(HttpClient httpClient, ILogger<EdtCaseManagementClient> logger) : base(httpClient, logger) { }

    public async Task<DigitalEvidenceCaseModel?> FindCase(string partyId, string caseName)
    {
        Serilog.Log.Information($"Case search on {caseName} by {partyId}");

        var result = await this.GetAsync<DigitalEvidenceCaseModel>($"case/{WebUtility.UrlEncode(partyId)}/{WebUtility.UrlEncode(caseName)}");

        if (!result.IsSuccess)
        {
            if (result.Status == DomainResults.Common.DomainOperationStatus.NotFound)
            {
                return new DigitalEvidenceCaseModel
                {
                    Name = caseName,
                    Status = "NotFound"
                };
            }
            else
            {
                return new DigitalEvidenceCaseModel
                {
                    Name = caseName,
                    Status = "Error",
                    Errors = string.Join(",", result.Errors)
                };
            }
        }

        return result.Value;

    }

    public async Task<DigitalEvidenceCaseModel?> GetCase(int caseId)
    {
        Serilog.Log.Information("Case requested by id {0}", caseId);

        var result = await this.GetAsync<DigitalEvidenceCaseModel>($"case/id/{caseId}");

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value;

    }
}
