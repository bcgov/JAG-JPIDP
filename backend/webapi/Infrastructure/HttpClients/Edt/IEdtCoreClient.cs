namespace Pidp.Infrastructure.HttpClients.Edt;

using Common.Models.EDT;

public interface IEdtCoreClient
{
    Task<List<EdtPersonDto>?> GetPersonsByIdentifier(string identitiferType, string identifierValue);
    Task<EdtPersonDto>? GetPersonByKey(string key);

}
