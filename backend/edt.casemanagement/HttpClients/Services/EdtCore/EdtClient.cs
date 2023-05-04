namespace edt.casemanagement.HttpClients.Services.EdtCore;

using System.Diagnostics.Metrics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoMapper;
using DomainResults.Common;
using edt.casemanagement.Exceptions;
using edt.casemanagement.Features.Cases;
using edt.casemanagement.Infrastructure.Telemetry;
using edt.casemanagement.Models;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;
using edt.casemanagement.ServiceEvents.CourtLocation.Models;
using edt.casemanagement.ServiceEvents.UserAccountCreation.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Serilog;

public class EdtClient : BaseClient, IEdtClient
{
    private readonly IMapper mapper;
    private readonly OtelMetrics meters;
    private readonly EdtServiceConfiguration configuration;
    private static readonly Counter ProcessedJobCount = Metrics
        .CreateCounter("case_search_count", "Number of case search requests.");


    public EdtClient(
        HttpClient httpClient, OtelMetrics meters, EdtServiceConfiguration edtServiceConfiguration,
        IMapper mapper,
        ILogger<EdtClient> logger)
        : base(httpClient, logger)
    {
        this.mapper = mapper;
        this.meters = meters;
        this.configuration = edtServiceConfiguration;
    }


    public async Task<EdtUserDto?> GetUser(string userKey)
    {

        this.meters.GetUser();
        Log.Logger.Information("Checking if user key {0} already present", userKey);
        var result = await this.GetAsync<EdtUserDto?>($"api/v1/users/key:{userKey}");

        if (!result.IsSuccess)
        {
            return null;
        }
        return result.Value;
    }

    public async Task<int> GetOuGroupId(string regionName)
    {
        IDomainResult<IEnumerable<OrgUnitModel?>>? result = await this.GetAsync<IEnumerable<OrgUnitModel?>>($"api/v1/org-units/1/groups");

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
        Log.Logger.Information("Got user cases {0} user {1}", result, userKey);

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
        var result = await this.GetAsync<IEnumerable<UserCaseGroup>>($"api/v1/cases/{caseId}/case-users/{userKey}/groups");
        Log.Logger.Information("Got user cases {0} user {1}", result, userKey);

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

        Log.Logger.Information("Adding user {0} to case {0}", userKey, caseId);
        var result = await this.PostAsync<JsonObject>($"api/v1/cases/{caseId}/case-users/{userKey}");

        if (result.IsSuccess)
        {
            Log.Information("Successfully added user {0} to case {1}", userKey, caseId);

            var caseGroupId = await this.GetCaseGroupId(caseId, this.configuration.EdtClient.SubmittingAgencyGroup);

            if (caseGroupId > 0)
            {
                // see if the user is already in the user/case/group combination
                var userCaseGroups = await this.GetUserCaseGroups(userKey, caseId);

                var existing = userCaseGroups.Any((caseGroup) => caseGroup.UserId == userKey && caseGroup.GroupId == caseGroupId);

                if (existing)
                {
                    Log.Information($"User {userKey} already assigned to case {caseId}");
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
        var result = await this.DeleteAsync($"api/v1/cases/{caseId}/case-users/remove/{userId}");

        if (result.IsSuccess)
        {
            Log.Information("Successfully removed user {0} from case {1}", userId, caseId);
        }
        else
        {
            Log.Error("Failed to remove user {0} from case {1} [{3}]", userId, caseId, string.Join(',', result.Errors));
        }
        return result.IsSuccess;
    }


    public async Task<CaseModel> FindCase(string caseIdOrKey)
    {
        //' /api/v1/org-units/1/cases/3:105: 23-472018/id
        ProcessedJobCount.Inc();
        var searchString = this.configuration.EdtClient.SearchFieldId + ":" + caseIdOrKey;
        Log.Logger.Information("Finding case {0}", searchString);

        var caseSearch = await this.GetAsync<IEnumerable<CaseLookupModel>?>($"api/v1/org-units/1/cases/{searchString}/id");

        if (caseSearch.IsSuccess)
        {
            var caseSearchValue = caseSearch?.Value;

            // Do something with the caseSearchValue

            if (caseSearchValue.Count() == 0)
            {
                Log.Information("No cases found for {0}", searchString);
                return null;
            }

            // see if we have multiple cases with the same id - if e do then ew want the one with a key
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
        else
        {
            throw new EdtServiceException(string.Join(",", caseSearch.Errors));

        }
    }




    public async Task<Task> HandleCaseRequest(string userKey, SubAgencyDomainEvent accessRequest)
    {
        // get the edt user

        var edtUser = await this.GetUser(userKey);

        if (edtUser == null)
        {
            throw new EdtServiceException("Invalid Edt user " + userKey);
        }

        try
        {

            if (accessRequest.EventType.Equals(CaseEventType.Provisioning, StringComparison.Ordinal))
            {
                Log.Information("Case provision request {0} {1}", userKey, accessRequest.CaseId);
                await this.AddUserToCase(edtUser.Id, accessRequest.CaseId);
            }
            else if (accessRequest.EventType.Equals(CaseEventType.Decommission, StringComparison.Ordinal))
            {
                Log.Information("Case decommission request {0} {1}", userKey, accessRequest.CaseId);
                await this.RemoveUserFromCase(edtUser.Id, accessRequest.CaseId);
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

        return Task.CompletedTask;
    }

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

  public async Task<Task> HandleCourtLocationRequest(string key, CourtLocationDomainEvent accessRequest)
  {
        Log.Debug($"handling court access request {accessRequest.RequestId}");

        return Task.CompletedTask;
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
