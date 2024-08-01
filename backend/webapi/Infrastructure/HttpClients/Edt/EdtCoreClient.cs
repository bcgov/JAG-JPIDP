namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Net;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.Models.EDT;
using Prometheus;

public class EdtCoreClient(HttpClient httpClient, ILogger<EdtCoreClient> logger) : BaseClient(httpClient, logger), IEdtCoreClient
{
    private static readonly Histogram PersonLookupByKey = Metrics.CreateHistogram("edt_user_lookup_by_key_duration", "Histogram of person key searches.");
    private static readonly Counter MergedUsersCounter = Metrics.CreateCounter("edt_merged_users_total", "Number of user queries returning merged users");
    private static readonly Counter NonMatchedUsersCount = Metrics.CreateCounter("edt_user_lookup_missing_total", "Number of user queries returning no users");
    private static readonly Counter PreExistingPersonsCount = Metrics.CreateCounter("edt_user_lookup_by_key_total", "Number of user queries returning a user from a key");

    public async Task<EdtPersonDto?> GetPersonByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (key.Length > 100)
        {
            throw new DIAMGeneralException($"Key too long for query GetPersonByKey({key})");
        }

        using (PersonLookupByKey.NewTimer())
        {
            Serilog.Log.Information($"Edt Person search requested by key {key}");

            var result = await this.GetAsync<EdtPersonDto?>($"person/key/{WebUtility.UrlEncode(key)}");

            if (result.IsSuccess)
            {

                Serilog.Log.Information($"Person found {result.Value.Id} for key {key}");
                PreExistingPersonsCount.Inc();
                return result.Value;
            }
            else
            {
                if (result.Status == DomainResults.Common.DomainOperationStatus.CriticalDependencyError)
                {
                    var errorMsg = $"Failed to lookup user by key [{string.Join(",", result.Errors)}";
                    Serilog.Log.Error(errorMsg);
                    throw new DIAMGeneralException(errorMsg);
                }
                Serilog.Log.Information($"No user found for key {key}");
                return null;
            }
        }
    }


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
                Serilog.Log.Information($"Checking possible person {person.LastName} {person.Id} for identifier {identifierValue}");
                if ((bool)!person.IsActive)
                {
                    Serilog.Log.Information($"User with identifier {identifierValue} is inactive - checking for PrimaryID for merged participants");
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

        Logger.LogWarning($"We did not find any primary user for source person {sourcePerson}");
        NonMatchedUsersCount.Inc();
        return null;
    }

    /// <summary>
    /// Find persons based on the lookup model
    /// </summary>
    /// <param name="personLookupModel"></param>
    /// <returns></returns>
    public async Task<List<EdtPersonDto>?> FindPersons(PersonLookupModel personLookupModel)
    {
        logger.LogInformation($"Edt Person search requested {personLookupModel.LastName} {personLookupModel.FirstName} {personLookupModel.DateOfBirth} ");

        var wrapper = new PersonLookupWrapper { PersonLookup = personLookupModel };

        var result = await this.PostAsync<List<EdtPersonDto>?>("person/search", wrapper);

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            logger.LogWarning($"Failed to query EDT  FindPersons {string.Join(",", result.Errors)}");
            return [];
        }
    }

    protected class PersonLookupWrapper
    {
        public PersonLookupModel? PersonLookup { get; set; }
    }
}
