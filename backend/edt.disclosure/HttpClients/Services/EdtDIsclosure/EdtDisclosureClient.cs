namespace edt.disclosure.HttpClients.Services.EdtDisclosure;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoMapper;

using edt.disclosure.Exceptions;
using edt.disclosure.Features.Cases;
using edt.disclosure.HttpClients.Services.EdtDIsclosure;
using edt.disclosure.Infrastructure.Telemetry;
using edt.disclosure.Kafka.Model;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.Models;
using edt.disclosure.ServiceEvents.UserAccountCreation.Models;
using Prometheus;
using Serilog;

public class EdtDisclosureClient : BaseClient, IEdtDisclosureClient
{
    private readonly IMapper mapper;
    private readonly OtelMetrics meters;
    private readonly EdtDisclosureServiceConfiguration configuration;
    private const string CounselGroup = "Counsel";
    private static readonly Counter ProcessedJobCount = Metrics
        .CreateCounter("disclosure_case_searches", "Number of disclosure case search requests.");
    private static readonly Histogram AccountCreationDuration = Metrics.CreateHistogram("edt_disclosure_account_creation_duration", "Histogram of edt disclosure account creations.");


    public EdtDisclosureClient(
        HttpClient httpClient, OtelMetrics meters, EdtDisclosureServiceConfiguration edtServiceConfiguration,
        IMapper mapper,
        ILogger<EdtDisclosureClient> logger)
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
            if (result.Status == DomainResults.Common.DomainOperationStatus.NotFound)
            {
                return null;
            }
            else
            {
                throw new EdtDisclosureServiceException($"Failed to query for user {userKey} - check service is available [{string.Join(",",result.Errors)}]");
            }
        }
        return result.Value;
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
            throw new EdtDisclosureServiceException(string.Join(",", result.Errors));
        }

        return result.Value.Version;
    }


    public async Task<Task> HandleCourtLocationRequest(string key, CourtLocationDomainEvent accessRequest)
    {
        Log.Debug($"handling court access request {accessRequest.RequestId}");

        // check user is present

        // get the id of the case


        return Task.CompletedTask;
    }

    public async Task<CourtLocationCaseModel> FindLocationCase(string caseName)
    {
        ProcessedJobCount.Inc();
        Log.Logger.Information("Finding case {0}", caseName);

        var caseSearch = await this.GetAsync<IEnumerable<CourtLocationCaseModel>?>($"api/v1/org-units/1/cases/{caseName}/id");

        var cases = caseSearch?.Value;
        int caseId;

        if (cases?.Count() > 1)
        {
            Log.Information("Multiple cases found for {0}", caseName);
            caseId = cases.FirstOrDefault(c => c.Key != null).Id;
        }
        else
        {
            caseId = cases.First().Id;
        }

        var result = await this.GetAsync<CourtLocationCaseModel?>($"api/v1/cases/{caseId}");

        if (result.IsSuccess)
        {
            return result?.Value;
        }
        else
        {
            throw new EdtDisclosureServiceException($"Failed to find case {caseName} : {string.Join(",", result.Errors)}");
        }

    }

    public async Task<UserModificationEvent> CreateUser(EdtDisclosureUserProvisioningModel accessRequest)
    {
        Log.Logger.Information($"Creating user case {accessRequest.Key} {accessRequest.FullName} {accessRequest.AccessRequestId}");

        using (AccountCreationDuration.NewTimer())
        {
            this.meters.AddUser();
            var edtUserDto = this.mapper.Map<EdtDisclosureUserProvisioningModel, EdtUserDto>(accessRequest);
            var result = await this.PostAsync($"api/v1/users", edtUserDto);
            var userModificationResponse = new UserModificationEvent
            {
                partId = edtUserDto.Key,
                eventType = UserModificationEvent.UserEvent.Create,
                eventTime = DateTime.Now,
                accessRequestId = accessRequest.AccessRequestId,
                successful = true
            };

            if (!result.IsSuccess)
            {
                Log.Logger.Error("Failed to create EDT user {0}", string.Join(",", result.Errors));
                userModificationResponse.successful = false;
            }

            //add user to group
            var newUser = await this.GetUser(accessRequest.Key!);

            if (newUser != null)
            {

                var groupAddResponse = await this.AddUserToOUGroup(newUser.Id, CounselGroup);
                if (!groupAddResponse)
                {
                    userModificationResponse.successful = false;
                    userModificationResponse.Errors.Add($"Failed to add user to group {CounselGroup}");
                }
                else
                {
                    Log.Error($"Did not found a group called {CounselGroup} in EDT");
                    userModificationResponse.successful = false;
                    userModificationResponse.Errors.Add($"Did not found a group called {CounselGroup} in EDT");
                }

                // add user to their folio folder
                var addedToFolio = await this.AddUserToFolio(accessRequest, newUser);
                if (!addedToFolio)
                {
                    userModificationResponse.successful = false;
                    userModificationResponse.Errors.Add($"Failed to add user to Folio");
                }
            }
            else
            {
                userModificationResponse.successful = false;
            }


            if (!userModificationResponse.successful)
            {
                Log.Warning($"User {newUser.Id} {newUser.Key} was not fully provisioned - disabling users account");
                var disabled = await this.EnableOrDisableAccount(newUser.Id, false);
                if (!disabled)
                {
                    userModificationResponse.Errors.Add($"Failed to disable user account");
                }
            }

            return userModificationResponse;


        }
    }

    private async Task<bool> AddUserToFolio(EdtDisclosureUserProvisioningModel accessRequest, EdtUserDto currentUser)
    {
        if (currentUser == null)
        {
            Log.Error($"Null user sent to AddUserToFolio({accessRequest.Id})");
            return false;
        }



        // lets confirm that the caseID and UniqueID match and nobody tried to fake it
        var caseById = await this.GetCase(accessRequest.FolioCaseId);
        var folioCase = await this.FindCase("Folio ID", accessRequest.FolioId);

        if (caseById.Id != folioCase.Id)
        {
            Log.Error($"Invalid attempt to access case - folio and case Id do not match - possible attempt to access unathorized cases");
            return false;
        }

        Log.Logger.Information($"Adding user {accessRequest.Email} to folio case {accessRequest.FolioId}");
        var completed = await this.AddUserToCase(currentUser.Id, folioCase.Id);


        if (!completed)
        {
            Log.Error($"Failed to add user {currentUser.Id} to case {folioCase.Id}");
            return false;
        }
        else
        {
            return true;
        }
    }


    private async Task<int> GetCaseGroupId(int caseId, string groupName)
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
            throw new EdtDisclosureServiceException("Failed to get groups for case " + caseId);
        }


    }

    private async Task<bool> AddUserToCase(string userKey, int caseId)
    {

        Log.Logger.Information("Adding user {0} to case {0}", userKey, caseId);
        var result = await this.PostAsync<JsonObject>($"api/v1/cases/{caseId}/case-users/{userKey}");

        if (result.IsSuccess)
        {
            Log.Information("Successfully added user {0} to case {1}", userKey, caseId);

            var caseGroupId = await this.GetCaseGroupId(caseId, CounselGroup);

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
                        Log.Error($"Failed to add user {userKey} to case group {CounselGroup} not assigned to case {caseId}");
                        throw new EdtDisclosureServiceException($"Failed to add user {userKey} to case group {CounselGroup} not assigned to case {caseId}");
                    }
                    else
                    {
                        Log.Information($"User {userKey} successfully added to group {caseGroupId} for case {caseId}");
                    }
                }
            }
            else
            {
                Log.Error($"{CounselGroup} not assigned to case {caseId}");
                throw new EdtDisclosureServiceException($"{CounselGroup} not assigned to case {caseId}");
            }
        }
        else
        {
            Log.Error("Failed to add user {0} to case {1} [{2}]", userKey, caseId, string.Join(',', result.Errors));
            throw new EdtDisclosureServiceException($"Failed to add user {userKey} to case {caseId} [{string.Join(',', result.Errors)}]");
        }

        return result.IsSuccess;
    }


    private async Task<IEnumerable<UserCaseGroup>> GetUserCaseGroups(string userKey, int caseId)
    {
        var result = await this.GetAsync<IEnumerable<UserCaseGroup>>($"api/v1/cases/{caseId}/case-users/{userKey}/groups");
        Log.Logger.Information("Got user cases {0} user {1}", result, userKey);

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            throw new EdtDisclosureServiceException($"Unable to get user case groups for user {userKey} and case {caseId}");
        }

    }


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

    public Task<UserModificationEvent> UpdateUser(EdtDisclosureUserProvisioningModel accessRequest, EdtUserDto currentUser)
    {
        throw new NotImplementedException();
    }

    public async Task<CaseModel> FindCase(string field, string value)
    {
        ProcessedJobCount.Inc();
        var searchString = Uri.EscapeDataString(field) + ':' + Uri.EscapeDataString(value);

        // limit search string length
        if (searchString.Length > 50)
        {
            throw new EdtDisclosureServiceException("Search string too large");
        }

        Log.Logger.Information("Finding case {0}", searchString);

        var caseSearch = await this.GetAsync<IEnumerable<CaseId>?>($"api/v1/org-units/1/cases/{searchString}/id");

        if (caseSearch.IsSuccess)
        {
            var caseSearchValue = caseSearch?.Value;

            // Do something with the caseSearchValue

            if (caseSearchValue.Count() == 0)
            {
                Log.Information("No cases found for {0}", searchString);
                return null;
            }

            // see if we have multiple cases with the same id - if e do then we want the one with a key
            var cases = caseSearch?.Value;
            int caseId;

            if (cases != null)
            {
                if (cases?.Count() > 1)
                {
                    Log.Information("Multiple cases found for {0}", searchString.ToString());
                    caseId = cases.FirstOrDefault(c => c.Key != null).Id;
                }
                else
                {
                    caseId = cases.First().Id;
                }

                return await this.GetCase(caseId);
            }
            else
            {
                return null;
            }

        }
        else
        {
            throw new EdtDisclosureServiceException(string.Join(",", caseSearch.Errors));

        }
    }


    public async Task<bool> EnableOrDisableAccount(string userIdOrKey, bool activateAccount)
    {
        Log.Logger.Information($"Account change active to {activateAccount} for user {userIdOrKey}");

        var user = await this.GetUser(userIdOrKey);

        if (user == null)
        {
            throw new EdtDisclosureServiceException($"User {userIdOrKey} does not exist for update");
        }
        else
        {

            user.IsActive = activateAccount;
            var result = await this.PutAsync($"api/v1/users", user);

            if (result.IsSuccess)
            {
                Log.Information($"Successfully update account for {userIdOrKey}");
                return true;
            }
            else
            {
                Log.Error($"Error occurred updating account for {userIdOrKey} [{string.Join(",", result.Errors)}]");
                return false;
            }

        }


    }

    public async Task<CaseModel> GetCase(int caseId)
    {
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
                throw new EdtDisclosureServiceException("No data found for case query");
            }

        }
        else
        {
            throw new EdtDisclosureServiceException(string.Join(",", result.Errors));
        }
    }

    private async Task<int> GetOuGroupId(string regionName)
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

    private async Task<List<EdtUserGroup>> GetAssignedOUGroups(string userKey)
    {
        var result = await this.GetAsync<List<EdtUserGroup>?>($"api/v1/org-units/1/users/{userKey}/groups");
        if (!result.IsSuccess)
        {
            Log.Logger.Error("Failed to determine existing EDT groups for {0} [{1}]", string.Join(", ", result.Errors));
            return null; //invalid
        }

        return result.Value;

    }

    private async Task<bool> AddUserToOUGroup(string userId, string groupName)
    {
        if (userId == null)
        {
            return false;
        }
        else
        {
            var groupId = await this.GetOuGroupId(groupName!);

            var result = await this.PostAsync($"api/v1/org-units/1/groups/{groupId}/users", new AddUserToOuGroup() { UserIdOrKey = userId });

            if (!result.IsSuccess)
            {
                Log.Logger.Error("Failed to add user {0} to region {1} due to {2}", userId, groupName, string.Join(",", result.Errors));
                return false;
            }
            else
            {
                Log.Logger.Information("Successfully added user {0} to group {1}", userId, groupName);
                return true;
            }
        }


    }


    public static class RequestEventType
    {
        public const string CourtAccessProvisionEvent = "digitalevidence-court-case-provision-event";
        public const string CourtAccessDecommissionEvent = "digitalevidence-court-case-decommission-event";
        public const string DisclosureUserProvisionEvent = "digitalevidence-disclosure-provision-event";
        public const string DisclosureUserDecommissionEvent = "igitalevidence-disclosure-decommission-event";
        public const string None = "None";
    }

    public class AddUserToOuGroup
    {
        public string? UserIdOrKey { get; set; }
    }

    public class CaseGroupModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
