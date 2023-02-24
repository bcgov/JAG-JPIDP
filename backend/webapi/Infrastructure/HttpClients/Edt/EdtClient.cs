namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Pidp.Models;

public class EdtClient : BaseClient, IEdtClient
{

    public EdtClient(HttpClient httpClient, ILogger<EdtClient> logger) : base(httpClient, logger) { }

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
