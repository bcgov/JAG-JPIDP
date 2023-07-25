namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Net;
using System.Threading.Tasks;
using Pidp.Models;

public class EdtDisclosureClient : BaseClient, IEdtDisclosureClient
{

    public EdtDisclosureClient(HttpClient httpClient, ILogger<EdtDisclosureClient> logger) : base(httpClient, logger) { }

    public async Task<DigitalEvidenceCaseModel?> FindFolio(int partyID, string folioID)
    {

        Serilog.Log.Information($"Party {partyID} Folio search requested {folioID}");

        var result = await this.GetAsync<DigitalEvidenceCaseModel>($"defence-disclosure/folio/{WebUtility.UrlEncode(folioID)}");

        if (!result.IsSuccess)
        {
            return null;
        }


        return result.Value;
    }

    public async Task<DigitalEvidenceCaseModel?> GetCaseModelByKey(string key)
    {

        var result = await this.GetAsync<DigitalEvidenceCaseModel>($"defence-disclosure/case/{key}");

        if (!result.IsSuccess)
        {
            return null;
        }


        return result.Value;
    }
}
