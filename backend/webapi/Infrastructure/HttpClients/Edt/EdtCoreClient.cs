namespace Pidp.Infrastructure.HttpClients.Edt;

using System.Net;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.Models.EDT;
using CommonModels.Models.Party;
using Pidp.Data;
using Prometheus;

public class EdtCoreClient(HttpClient httpClient, ILogger<EdtCoreClient> logger, PidpDbContext dbContext, IEdtCaseManagementClient edtCaseManagement) : BaseClient(httpClient, logger), IEdtCoreClient
{
    private static readonly Histogram PersonLookupByKey = Metrics.CreateHistogram("edt_person_lookup_by_key_duration", "Histogram of person key searches.");
    private static readonly Histogram UserLookupByKey = Metrics.CreateHistogram("edt_user_lookup_by_key_duration", "Histogram of user key searches.");
    private static readonly Histogram UserCasesLookup = Metrics.CreateHistogram("edt_user_cases_lookup_by_key_duration", "Histogram of user case searches.");

    private static readonly Counter MergedUsersCounter = Metrics.CreateCounter("edt_merged_users_total", "Number of user queries returning merged users");
    private static readonly Counter NonMatchedUsersCount = Metrics.CreateCounter("edt_user_lookup_missing_total", "Number of user queries returning no users");
    private static readonly Counter PreExistingPersonsCount = Metrics.CreateCounter("edt_person_lookup_by_key_total", "Number of person queries returning a user from a key");

    /// <summary>
    /// Retrieves a person from EDT based on the provided key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The <see cref="EdtPersonDto"/> if found; otherwise, null.</returns>
    public async Task<EdtPersonDto?> GetPersonByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

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

    /// <summary>
    /// Retrieves a list of persons from EDT based on the provided identifier type and value.
    /// </summary>
    /// <param name="identitiferType">The identifier type.</param>
    /// <param name="identifierValue">The identifier value.</param>
    /// <returns>The list of <see cref="EdtPersonDto"/> if found; otherwise, null.</returns>
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
                if (!person.IsActive)
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourcePerson"></param>
    /// <returns></returns>
    private async Task<EdtPersonDto> GetPrimaryPerson(EdtPersonDto sourcePerson)
    {
        var primaryIDCount = sourcePerson.Identifiers.Select(identifier => identifier.IdentifierType.Equals("PrimaryID")).ToList().Count;
        if (sourcePerson.IsActive && primaryIDCount == 0)
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
    /// Finds persons based on the provided person lookup model.
    /// </summary>
    /// <param name="personLookupModel">The person lookup model.</param>
    /// <returns>The list of <see cref="EdtPersonDto"/> if found; otherwise, an empty list.</returns>
    public async Task<List<EdtPersonDto>?> FindPersons(PersonLookupModel personLookupModel)
    {
        logger.LogInformation($"Edt Person search requested {personLookupModel} ");

        var wrapper = new PersonLookupWrapper { PersonLookup = personLookupModel };

        var result = await this.PostAsync<List<EdtPersonDto>?>("person/search", wrapper);

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            logger.LogWarning($"Failed to query EDT  FindPersons {string.Join(",", result.Errors)}");
            return new List<EdtPersonDto>();
        }
    }

    /// <summary>
    /// Retrieves a user from EDT based on the provided key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The <see cref="EdtUserDto"/> if found; otherwise, null.</returns>
    public async Task<EdtUserDto?> GetUserByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (key.Length > 100)
        {
            throw new DIAMGeneralException($"Key too long for query GetUserByKey({key})");
        }

        using (UserLookupByKey.NewTimer())
        {
            var result = await this.GetAsync<EdtUserDto?>($"user/party/{key}");

            if (result.IsSuccess)
            {
                return result.Value;
            }
            else
            {
                logger.LogWarning($"Failed to query EDT  GetUser {string.Join(",", result.Errors)}");
                return null;
            }
        }
    }



    /// <summary>
    /// Retrieves a list of user cases from EDT based on the provided user ID.
    /// </summary>
    /// <param name="userId">The user ID to search for.</param>
    /// <returns>The list of <see cref="UserCaseSearchResponseModel"/> if found; otherwise, null.</returns>
    public async Task<List<UserCaseSearchResponseModel>?> GetUserCases(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        if (userId.Length > 100)
        {
            throw new DIAMGeneralException($"User Id too long for query GetUserCases({userId})");
        }

        using (UserCasesLookup.NewTimer())
        {
            var result = await this.GetAsync<List<UserCaseSearchResponseModel>?>($"user/party/cases/{userId}");

            if (result.IsSuccess)
            {
                return result.Value;
            }
            else
            {
                logger.LogWarning($"Failed to query EDT for user cases {string.Join(",", result.Errors)}");
                return null;
            }
        }
    }

    /// <summary>
    /// Get merge info a given participant - needed to check for disclosures for a given user when establishing accounts
    /// </summary>
    /// <param name="participantId"></param>
    /// <returns></returns>
    public async Task<ParticipantMergeListingModel> GetParticipantMergeListing(string participantId)
    {
        Logger.LogInformation($"Getting participant merge info for {participantId}");

        if (string.IsNullOrEmpty(participantId))
        {
            throw new DIAMGeneralException($"No participant ID in call to GetParticipantMergeListing()");
        }

        var result = await this.GetAsync<ParticipantMergeListingModel>($"participant/merge-details/{participantId}");

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            logger.LogWarning($"Failed to query EDT for user merge info for {participantId} - {string.Join(",", result.Errors)}");
            return null;
        }
    }

    protected class PersonLookupWrapper
    {
        public PersonLookupModel? PersonLookup { get; set; }
    }
}
