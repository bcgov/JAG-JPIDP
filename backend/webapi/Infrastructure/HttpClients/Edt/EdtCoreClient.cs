namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Threading.Tasks;
using Common.Exceptions;
using Common.Models.EDT;

public class EdtCoreClient : BaseClient, IEdtCoreClient
{

    public EdtCoreClient(HttpClient httpClient, ILogger<EdtCoreClient> logger) : base(httpClient, logger) { }

    public async Task<List<EdtPersonDto>?> GetPersonsByIdentifier(string identitiferType, string identifierValue)
    {
        Serilog.Log.Information($"Edt Person search requested {identitiferType} {identifierValue}");

        var result = await this.GetAsync<List<EdtPersonDto?>>($"person/identifier/{identitiferType}/{identifierValue}");

        if (!result.IsSuccess)
        {
            Serilog.Log.Error($"Failed to query EDT  GetPersonsByIdentifier {identitiferType} {identifierValue} {string.Join(",", result.Errors)}");
            throw new DIAMGeneralException(string.Join(",", result.Errors));
        }


        return result.Value;
    }
}
