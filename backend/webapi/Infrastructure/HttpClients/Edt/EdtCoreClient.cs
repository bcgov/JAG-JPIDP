namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Threading.Tasks;
using Common.Exceptions;
using Common.Models.EDT;
using Prometheus;

public class EdtCoreClient : BaseClient, IEdtCoreClient
{

    public EdtCoreClient(HttpClient httpClient, ILogger<EdtCoreClient> logger) : base(httpClient, logger) { }

    private static readonly Counter MergedUsersCounter = Metrics.CreateCounter("edt_merged_users_total", "Number of user queries returning merged users");
    private static readonly Counter NonMatchedUsersCount = Metrics.CreateCounter("edt_user_lookup_missing_total", "Number of user queries returning no users");


    public async Task<List<EdtPersonDto>?> GetPersonsByIdentifier(string identitiferType, string identifierValue)
    {
        Serilog.Log.Information($"Edt Person search requested {identitiferType} {identifierValue}");

        var result = await this.GetAsync<List<EdtPersonDto?>>($"person/identifier/{identitiferType}/{identifierValue}");
        var personList = new List<EdtPersonDto>();
        if (!result.IsSuccess)
        {
            Serilog.Log.Error($"Failed to query EDT  GetPersonsByIdentifier {identitiferType} {identifierValue} {string.Join(",", result.Errors)}");
            throw new DIAMGeneralException(string.Join(",", result.Errors));
        }
        else
        {
            // Check for merged participants - if record has a primary ID and is inactive then we need to fetch the primary record
            foreach (var person in result.Value)
            {
                if ((bool)!person.IsActive)
                {
                    var primaryPerson = await this.GetPrimaryPerson(person);
                    if (primaryPerson != null)
                    {
                        Serilog.Log.Information($"Person {primaryPerson.Id} is the primary - returning this user for identifier {identitiferType}-{identifierValue}");
                        personList.Add(primaryPerson);
                    }
                }
                else
                {
                    personList.Add(person);
                }
            }

        }


        return personList;
    }

    private async Task<EdtPersonDto> GetPrimaryPerson(EdtPersonDto sourcePerson)
    {

        var primaryIDCount = sourcePerson.Identifiers.Select(identifier => identifier.IdentifierType.Equals("PrimaryID")).ToList().Count;
        if ((bool)sourcePerson.IsActive && primaryIDCount == 0)
        {
            return sourcePerson;
        }

        Serilog.Log.Information($"Checking inactive person {sourcePerson.Id}");
        var primaryID = sourcePerson.Identifiers.First(identifier => identifier.IdentifierType.Equals("PrimaryID"));

        if (primaryID != null)
        {
            MergedUsersCounter.Inc();
            Serilog.Log.Information($"Person {sourcePerson.Id} has been merged with record {primaryID.IdentifierValue}");
            var primaryPerson = await this.GetAsync<EdtPersonDto?>($"person/{sourcePerson.Id}");
            if (primaryPerson != null)
            {
                return await this.GetPrimaryPerson(primaryPerson.Value);
            }

        }

        Serilog.Log.Warning($"We did not find any primary user for source person {sourcePerson}");
        NonMatchedUsersCount.Inc();
        return null;
    }
}
