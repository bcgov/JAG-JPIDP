namespace edt.casemanagement.HttpClients.Services.EdtCore;

using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using AutoMapper;
using Common.Models.EDT;
using edt.casemanagement.Data;
using edt.casemanagement.Exceptions;
using edt.casemanagement.Features.Cases;
using edt.casemanagement.Infrastructure.Telemetry;
using edt.casemanagement.Models;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;
using Microsoft.Extensions.Logging;
using NodaTime;
using Serilog;

public class EdtClient(
    HttpClient httpClient, Instrumentation instrumentation, EdtServiceConfiguration edtServiceConfiguration,
    IMapper mapper,
    IClock clock,
    CaseManagementDataStoreDbContext context,
    ILogger<EdtClient> logger) : BaseClient(httpClient, logger), IEdtClient
{
    private readonly IMapper mapper = mapper;
    private readonly IClock clock = clock;

    private readonly EdtServiceConfiguration configuration = edtServiceConfiguration;
    private readonly CaseManagementDataStoreDbContext context = context;


    private readonly Counter<long> searchCount = instrumentation.CaseSearchCount;
    private readonly Counter<long> processedJobCount = instrumentation.ProcessedJobCount;
    private readonly Counter<long> processRemovedJob = instrumentation.ProcessRemovedJob;
    private readonly Histogram<double> caseStatusDuration = instrumentation.CaseStatusDuration;


    public async Task<EdtUserDto?> GetUser(string userKey)
    {

        Log.Information("Checking if user key {0} already present", userKey);
        var result = await this.GetAsync<EdtUserDto?>($"api/v1/users/key:{userKey}");

        if (!result.IsSuccess)
        {
            return null;
        }
        return result.Value;
    }

    public async Task<int> GetOuGroupId(string regionName)
    {
        var result = await this.GetAsync<IEnumerable<OrgUnitModel?>>($"api/v1/org-units/1/groups");

        if (!result.IsSuccess)
        {
            return 0; //invalid
        }

        return result.Value!
            .Where(ou => ou!.Name == regionName)
            .Select(ou => ou!.Id)
            .FirstOrDefault();
    }





    /// <summary>
    /// Get the current EDT version
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EdtServiceException"></exception>
    public async Task<string> GetVersion()
    {
        var result = await this.GetAsync<EdtVersion?>($"api/v1/version");

        if (!result.IsSuccess)
        {
            throw new EdtServiceException(string.Join(",", result.Errors));
        }

        return result.Value.Version;
    }


    public async Task<IEnumerable<KeyIdPair>> GetUserCases(string userKey)
    {
        var result = await this.GetAsync<IEnumerable<KeyIdPair>>($"api/v1/org-units/1/users/{userKey}/cases");
        Log.Information("Got user cases {0} user {1}", result, userKey);

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            throw new CaseAssignmentException($"Unable to get user cases for user {userKey}");
        }
    }

    public async Task<IEnumerable<UserCaseGroup>> GetUserCaseGroups(string userKey, int caseId)
    {
        var result = await this.GetAsync<IEnumerable<UserCaseGroup>>($"api/v1/cases/{caseId}/users/{userKey}/groups");
        Log.Information("Got user cases {0} user {1}", result, userKey);

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            throw new CaseAssignmentException($"Unable to get user case groups for user {userKey} and case {caseId}");
        }

    }


    public async Task<bool> AddUserToCase(string userKey, int caseId)
    {

        // make sure user isnt already added to case
        var existingUsers = await this.GetAsync<CaseUsersModel>($"api/v1/cases/{caseId}/users");

        if (!existingUsers.IsSuccess)
        {
            Log.Error($"Failed to get existing users for case {caseId} [{string.Join(",", existingUsers.Errors)}");
            return false;

        }

        var alreadyCaseUser = existingUsers.Value.CaseUsers.FirstOrDefault(user => user.UserId.Equals(userKey));

        if (alreadyCaseUser != null)
        {
            Log.Information($"User {userKey} {alreadyCaseUser.UserName} already exists on case {caseId}");
            return true;
        }

        Log.Information($"Adding user {userKey} to case {caseId}");
        var result = await this.PostAsync($"api/v1/cases/{caseId}/users/{userKey}");

        if (result.IsSuccess)
        {
            this.processedJobCount.Add(1);
            Log.Information("Successfully added user {0} to case {1}", userKey, caseId);

            var caseGroupId = await this.GetCaseGroupId(caseId, this.configuration.EdtClient.SubmittingAgencyGroup);

            if (caseGroupId > 0)
            {
                // see if the user is already in the user/case/group combination
                var userCaseGroups = await this.GetUserCaseGroups(userKey, caseId);

                var existing = userCaseGroups.Any((caseGroup) => caseGroup.UserId == userKey && caseGroup.GroupId == caseGroupId);

                if (existing)
                {
                    Log.Information($"User {userKey} already assigned to case {caseId} with group {caseGroupId}");
                }
                else
                {

                    var addUserToCaseGroup = await this.AddUserToCaseGroup(userKey, caseId, caseGroupId);
                    if (!addUserToCaseGroup)
                    {
                        Log.Error($"Failed to add user {userKey} to case group {this.configuration.EdtClient.SubmittingAgencyGroup} not assigned to case {caseId}");
                        throw new CaseAssignmentException($"Failed to add user {userKey} to case group {this.configuration.EdtClient.SubmittingAgencyGroup} not assigned to case {caseId}");
                    }
                    else
                    {
                        Log.Information($"User {userKey} successfully added to group {caseGroupId} for case {caseId}");
                    }
                }
            }
            else
            {
                Log.Error($"{this.configuration.EdtClient.SubmittingAgencyGroup} not assigned to case {caseId}");
                throw new CaseAssignmentException($"{this.configuration.EdtClient.SubmittingAgencyGroup} not assigned to case {caseId}");
            }
        }
        else
        {
            Log.Error("Failed to add user {0} to case {1} [{2}]", userKey, caseId, string.Join(',', result.Errors));
            throw new CaseAssignmentException($"Failed to add user {userKey} to case {caseId} [{string.Join(',', result.Errors)}]");
        }

        return result.IsSuccess;
    }


    public async Task<bool> RemoveUserFromCase(string userId, int caseId)
    {
        // var result = await this.PostAsync<>($"api/v1/version");
        var result = await this.DeleteAsync($"api/v1/cases/{caseId}/users/{userId}");

        if (result.IsSuccess)
        {
            this.processRemovedJob.Add(1);
            Log.Information("Successfully removed user {0} from case {1}", userId, caseId);
        }
        else
        {
            Log.Error("Failed to remove user {0} from case {1} [{3}]", userId, caseId, string.Join(',', result.Errors));
        }
        return result.IsSuccess;
    }

    public async Task<CaseModel> GetCase(int caseId)
    {

        // get the case status - later this will be redundant as it can be done with the find query to get the Id (11.6.8?)
        var caseStatus = await this.GetCaseSummary(caseId);

        if (caseStatus != null)
        {
            // check the status of the case - if not active then we wont attempt to get the case by ID as this will fail
            if (caseStatus.Status != "Active")
            {
                if (caseStatus.Status == "Queued")
                {
                    this.Logger.LogCaseIsBeingCreated(caseId);

                    // this case is currently being created and looking up the case details will fail at this time
                    return new CaseModel
                    {
                        Status = caseStatus.Status,
                        Key = caseStatus.Key,
                        Id = caseStatus.Id,
                        Name = caseStatus.Name
                    };
                }
                else
                {
                    this.Logger.LogCaseStatusNotActive(caseId, caseStatus.Status);
                }
            }
        }

        var result = await this.GetAsync<CaseModel?>($"api/v1/cases/{caseId}");
        if (result.IsSuccess)
        {
            if (result != null)
            {
                // filter out unwanted data
                var customFieldsArray = this.configuration.CaseDisplayCustomFields.Select(f => f.Id).ToArray();
                var customFieldsIds = this.configuration.CaseDisplayCustomFields.Select(f => f.Id).ToList();
                var filteredFields = result.Value.Fields.Where(f => customFieldsIds.Contains(f.Id)).ToList();

                var removeValues = new List<int>();

                foreach (var field in filteredFields)
                {
                    var defn = this.configuration.CaseDisplayCustomFields.FirstOrDefault(f => f.Id == field.Id);
                    if (defn.RelatedId > 0)
                    {
                        var relatedField = result.Value.Fields.FirstOrDefault(f => f.Id == defn.RelatedId);
                        if (relatedField != null)
                        {
                            if (defn.RelatedValueEmpty)
                            {
                                if (relatedField.Value != null && !string.IsNullOrEmpty(relatedField.Value.ToString()))
                                {
                                    removeValues.Add(field.Id);
                                }
                            }
                            else
                            {
                                if (relatedField.Value == null || string.IsNullOrEmpty(relatedField.Value.ToString()))
                                {
                                    removeValues.Add(field.Id);
                                }
                            }
                        }
                    }

                    if (!defn.Display && !removeValues.Contains(field.Id))
                    {
                        //removeValues.Add(field.Id);
                        field.Display = false;
                    }
                }

                if (removeValues.Count > 0)
                {
                    filteredFields = filteredFields.Where(f => !removeValues.Contains(f.Id)).ToList();
                }

                filteredFields.ForEach(f =>
                {
                    if (f.Value == null || f.Value.ToString() == "null")
                    {
                        f.Value = "Not set";
                    }
                });


                filteredFields.Sort((f1, f2) =>
                {
                    var index1 = Array.IndexOf(customFieldsArray, f1.Id);
                    var index2 = Array.IndexOf(customFieldsArray, f2.Id);
                    return index1.CompareTo(index2);
                });

                result.Value.Fields = filteredFields;
                return result.Value;
            }
            else
            {
                throw new EdtServiceException("No data found for case query");
            }

        }
        else
        {
            throw new EdtServiceException(string.Join(",", result.Errors));
        }
    }


    public async Task<CaseModel> FindCase(CaseLookupQuery query)
    {

        var caseIdOrKey = query.caseName;
        //' /api/v1/org-units/1/cases/3:105: 23-472018/id
        this.searchCount.Add(1);


        if (this.configuration.SearchFieldId == -1 || this.configuration.AlternateSearchFieldId == -1)
        {
            Log.Fatal("Unable to determine search fields based on config - please check configuration");
            Environment.Exit(1);
        }

        var searchString = this.configuration.SearchFieldId + ":" + caseIdOrKey;
        Log.Logger.Information("Finding case {0}", searchString);


        var watch = System.Diagnostics.Stopwatch.StartNew();

        // record case search request
        var searchRequest = new CaseSearchRequest { AgencyFileNumber = caseIdOrKey, SearchString = searchString, PartyId = query.partyId, Created = this.clock.GetCurrentInstant() };
        this.context.Add(searchRequest);

        CaseModel foundCase = null;
        var caseSearch = await this.GetAsync<IEnumerable<CaseLookupModel>?>($"api/v1/org-units/1/cases/{searchString}/id");


        try
        {
            if (caseSearch.IsSuccess)
            {
                var caseSearchValue = caseSearch?.Value;

                // Do something with the caseSearchValue

                if (!caseSearchValue.Any())
                {
                    Log.Information("No cases found for {0}", searchString);


                    // check for merged - switch to alternate search
                    if (this.configuration.AlternateSearchFieldId > 0)
                    {
                        searchString = this.configuration.AlternateSearchFieldId + ":" + caseIdOrKey;

                        Log.Information($"Searching by alternate Id [{searchString}]");

                        var alternateSearch = await this.GetAsync<IEnumerable<CaseLookupModel>?>($"api/v1/org-units/1/cases/{searchString}/id");
                        if (alternateSearch.IsSuccess)
                        {
                            var alternateSearchValue = alternateSearch?.Value;
                            if (alternateSearchValue.Any())
                            {
                                foreach (var alternateCase in alternateSearchValue)
                                {
                                    var caseInfo = await this.GetCase(alternateCase.Id);

                                    if (caseInfo != null && caseInfo.Status == "Active")
                                    {
                                        Log.Information($"Using primary case {caseInfo.Id} [{caseInfo.Status}] [{caseInfo.Name}]");
                                        foundCase = caseInfo;
                                        break;
                                    }
                                    else
                                    {
                                        Log.Information($"Found inactive case {caseInfo.Id} [{caseInfo.Status}] [{caseInfo.Name}]");

                                    }
                                }

                            }
                        }
                    }


                    searchRequest.ResponseStatus = (foundCase != null) ? "Found " + foundCase.Id : "Not found";

                    // As we're searching a 200 is a valid response for a non-existent case
                    if (foundCase == null)
                    {
                        return new CaseModel
                        {
                            Status = EDTCaseStatus.NotFound.ToString(),
                            Name = caseIdOrKey,

                        };
                    }
                    return foundCase;
                }

                // see if we have multiple cases with the same id - if we do then we want the one with a key
                var cases = caseSearch?.Value;
                int caseId;

                if (cases.Count() > 1)
                {
                    Log.Information("Multiple cases found for {0}", searchString.ToString());
                    caseId = cases.FirstOrDefault(c => c.Key != null).Id;
                }
                else
                {
                    caseId = cases.First().Id;
                }



                foundCase = await this.GetCase(caseId);

                if (foundCase.Status == "Inactive")
                {
                    var primaryAgencyFileField = foundCase?.Fields.First(field => field.Name.Equals("Primary Agency File ID"));
                    if (primaryAgencyFileField.Value != null && !string.IsNullOrEmpty(primaryAgencyFileField.Value.ToString()))
                    {
                        Log.Information($"Checking if case {caseId} was merged - primary file id {primaryAgencyFileField.Value.ToString()}");
                        var primarySearchString = primaryAgencyFileField.Id + ":" + primaryAgencyFileField.Value.ToString();

                        var primarySearch = await this.GetAsync<IEnumerable<CaseLookupModel>?>($"api/v1/org-units/1/cases/{primarySearchString}/id");

                        if (primarySearch.IsSuccess)
                        {
                            if (primarySearch.Value != null)
                            {
                                Log.Information($"Found {primarySearch.Value.Count()} cases with primary ID {primarySearchString}");

                                foreach (var caseModel in primarySearch.Value)
                                {
                                    if (caseModel.Id == caseId)
                                    {
                                        Log.Information($"Ignoring case {caseId} as it was our searched case");
                                        continue;
                                    }
                                    else
                                    {
                                        var testCase = await this.GetCase(caseModel.Id);
                                        if (testCase != null && testCase.Status == "Active")
                                        {
                                            // check case has the original agency file number
                                            var agencyFileNumber = testCase.Fields.First(c => c.Name == "Agency File No.");
                                            if (agencyFileNumber != null && agencyFileNumber.Value != null && !string.IsNullOrEmpty(agencyFileNumber.Value.ToString()))
                                            {
                                                Log.Information($"Returning alternate case {caseModel.Id}");
                                                foundCase = testCase;
                                                break;
                                            }
                                        }

                                    }

                                }

                            }


                        }
                    }
                }

                searchRequest.ResponseStatus = "Found " + foundCase.Id;


                return foundCase;

            }
            else
            {


                var errors = string.Join(",", caseSearch.Errors);
                searchRequest.ResponseStatus = $"Errors: {errors}";

                return new CaseModel
                {
                    Errors = errors,
                    Id = -1
                };

            }

        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Case {caseIdOrKey} lookup error [{ex.Message}]");
            searchRequest.ResponseStatus = ex.Message;
            return foundCase;

        }
        finally
        {
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            searchRequest.ResponseTime = elapsedMs;

            await this.context.SaveChangesAsync();
        }

    }

    /// <summary>
    /// get custom case fields
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CustomFieldDefinition>> GetCustomFields(string objectType)
    {
        Log.Information($"Getting all field info for type {objectType}");

        var result = await this.GetAsync<IEnumerable<CustomFieldDefinition>?>($"api/v1/org-units/1/fields");

        if (result.IsSuccess && result.Value != null)
        {
            return result.Value.Where(c => c.ObjectType == objectType);

        }

        return null;
    }


    /// <summary>
    /// Command handler
    /// </summary>
    /// <param name="userKey"></param>
    /// <param name="accessRequest"></param>
    /// <returns></returns>
    /// <exception cref="EdtServiceException"></exception>
    public async Task<Task> HandleCaseRequest(string userKey, SubAgencyDomainEvent accessRequest)
    {
        // get the edt user

        var edtUser = await this.GetUser(userKey);
        var response = false;

        if (edtUser == null)
        {
            throw new EdtServiceException("Invalid Edt user " + userKey);
        }

        try
        {

            if (accessRequest.EventType.Equals(CaseEventType.Provisioning, StringComparison.Ordinal))
            {
                Log.Information("Case provision request {0} {1}", userKey, accessRequest.CaseId);
                response = await this.AddUserToCase(edtUser.Id, accessRequest.CaseId);
            }
            else if (accessRequest.EventType.Equals(CaseEventType.Decommission, StringComparison.Ordinal))
            {
                Log.Information("Case decommission request {0} {1}", userKey, accessRequest.CaseId);
                response = await this.RemoveUserFromCase(edtUser.Id, accessRequest.CaseId);
            }
            else
            {
                throw new EdtServiceException("Invalid case request event type " + accessRequest.EventType);
            }
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        if (!response)
        {
            return Task.FromException(new CaseAssignmentException($"Failed to process {accessRequest.AgencyFileNumber} request for {userKey} event {accessRequest.EventType}"));
        }
        return Task.CompletedTask;
    }


    /// <summary>
    /// Get the group id assigned to the case
    /// </summary>
    /// <param name="caseId"></param>
    /// <param name="groupName"></param>
    /// <returns></returns>
    /// <exception cref="CaseAssignmentException"></exception>
    public async Task<int> GetCaseGroupId(int caseId, string groupName)
    {
        Log.Debug("Getting case {0} group id for {1}", caseId, groupName);

        var result = await this.GetAsync<IEnumerable<CaseGroupModel>?>($"api/v1/cases/{caseId}/groups");

        if (result.IsSuccess)
        {
            var matchingGroup = result?.Value?.FirstOrDefault(group => group.Name.Equals(groupName, StringComparison.Ordinal));
            if (matchingGroup != null)
            {
                return matchingGroup.Id;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            throw new CaseAssignmentException("Failed to get groups for case " + caseId);
        }


    }

    /// <summary>
    /// Add a user to a case group
    /// {{host}}/api/v1/cases/{{caseId}}/groups/1/users/{{userId}}
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="caseId"></param>
    /// <param name="caseGroupId"></param>
    /// <returns></returns>

    public async Task<bool> AddUserToCaseGroup(string userId, int caseId, int caseGroupId)
    {
        Log.Debug("Adding user {0} in case {1} to group {2}", userId, caseId, caseGroupId);

        var result = await this.PostAsync($"api/v1/cases/{caseId}/groups/{caseGroupId}/users/{userId}");

        if (result.IsSuccess)
        {
            Log.Information("Added user {0} to group {1} for case {2}", userId, caseGroupId, caseId);
            return true;
        }
        else
        {
            Log.Error("Failed to add user {0} to group {1} for case {2} [{3}]", userId, caseGroupId, caseId, string.Join(',', result.Errors));
            return false;
        }

    }

    /// <summary>
    /// Used to get the summary of a case - can be used to determine if case is currently in the process of being generated
    /// </summary>
    /// <param name="caseId"></param>
    /// <returns></returns>
    public async Task<CaseSummaryModel> GetCaseSummary(int caseId)
    {


        Log.Debug($"Getting case info for {caseId}");

        var result = await this.GetAsync<CaseSummaryModel>($"api/v1/org-units/1/cases/info/{caseId}");
        var watch = System.Diagnostics.Stopwatch.StartNew();

        if (result.IsSuccess)
        {
            watch.Stop();

            Log.Information($"Case {caseId} status is {result.Value.Status}");
            this.caseStatusDuration.Record(watch.ElapsedMilliseconds);
            return result.Value;
        }
        else
        {
            watch.Stop();

            return null;
        }


    }

    public static class CaseEventType
    {
        public const string Provisioning = "case-provision-event";
        public const string Decommission = "case-decommission-event";
        public const string None = "None";
    }

    public class CaseGroupModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }


}

public static partial class EdtClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Case {caseId} status is {status} and is not active.")]
    public static partial void LogCaseStatusNotActive(this Microsoft.Extensions.Logging.ILogger logger, int caseId, string status);
    [LoggerMessage(2, LogLevel.Information, "Case {caseId} status currently being created and will be available shortly.")]
    public static partial void LogCaseIsBeingCreated(this Microsoft.Extensions.Logging.ILogger logger, int caseId);
}

