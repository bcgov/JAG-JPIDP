namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Pidp.Models;

public class EdtCaseManagementClient : BaseClient, IEdtCaseManagementClient
{

    public EdtCaseManagementClient(HttpClient httpClient, ILogger<EdtCaseManagementClient> logger) : base(httpClient, logger) { }

    public async Task<DigitalEvidenceCaseModel?> FindCase(string caseName)
    {
        Serilog.Log.Information("Case search requested {0}", caseName);

        var result = await this.GetAsync<DigitalEvidenceCaseModel>($"case/{WebUtility.UrlEncode(caseName)}");

        if (!result.IsSuccess)
        {
            return null;
        }


        return result.Value;


    }
}
