namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

using System.Dynamic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoMapper;

using edt.disclosure.Exceptions;
using edt.disclosure.Features.Cases;
using edt.disclosure.Infrastructure.Telemetry;
using edt.disclosure.Kafka.Model;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.Models;
using edt.disclosure.ServiceEvents.UserAccountCreation.Models;
using Google.Protobuf.WellKnownTypes;
using MediatR;
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
    private static readonly Histogram AccountUpdateDuration = Metrics.CreateHistogram("edt_disclosure_account_update_duration", "Histogram of edt disclosure account updates.");
    private static readonly Counter CaseAddRequest = Metrics
    .CreateCounter("disclosure_case_additions", "Number of disclosure cases added.");

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
                Log.Logger.Information($"User {userKey} not found");
                return null;
            }
            else
            {
                throw new EdtDisclosureServiceException($"Failed to query for user {userKey} - check service is available [{string.Join(",", result.Errors)}]");
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
            throw new EdtDisclosureServiceException($"Failed to communicate with EDT {string.Join(",", result.Errors)}");
        }

        return result.Value.Version;
    }


    public async Task<Task> HandleCourtLocationRequest(string key, CourtLocationDomainEvent accessRequest)
    {
        Log.Information($"Handling court access request {accessRequest.RequestId}");

        // check user is present
        var user = await this.GetUser(accessRequest.Username);

        if (user == null)
        {
            throw new EdtDisclosureServiceException($"User [{accessRequest.Username}] not present to assign cases");
        }
        // get the id of the case

        Log.Information("USING DUMMY COURT LOCATION Victoria Courthouse - Test");
        var courtLocation = await FindLocationCase("CH-VIC");

        if (courtLocation == null)
        {
            throw new EdtDisclosureServiceException($"Unable to determine court location with name {accessRequest.CourtLocation}");
        }
        else
        {
            Log.Information($"Adding user {user.Id} to court location {courtLocation.Id}");
            var addedOk = await this.AddUserToCase(user.Id, courtLocation.Id, "Reviewers");
            if (addedOk)
            {
                Log.Information($"Added user {user.Id} to court location {courtLocation.Id}");
            }
            else
            {
                Log.Error($"Failed to add user {user.Id} to court location {courtLocation.Id}");

            }

        }



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
        Log.Logger.Information($"Creating disclosure user {accessRequest.Key} {accessRequest.FullName} {accessRequest.AccessRequestId}");

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
                    Log.Information($"User {newUser.Id} added to {CounselGroup} in EDT");
                }

                //// add user to their folio folder
                //var addedToFolio = await this.AddUserToFolio(accessRequest, newUser);
                //if (!addedToFolio)
                //{
                //    userModificationResponse.successful = false;
                //    userModificationResponse.Errors.Add($"Failed to add user to Folio");
                //}
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

    /// <summary>
    /// User date the user
    /// </summary>
    /// <param name="accessRequest"></param>
    /// <param name="currentUser"></param>
    /// <returns></returns>
    public async Task<UserModificationEvent> UpdateUser(EdtDisclosureUserProvisioningModel accessRequest, EdtUserDto currentUser)
    {

        if (currentUser == null || accessRequest == null)
        {
            throw new EdtDisclosureServiceException("Null user or access request passed to UpdateUser()");
        }

        using (AccountUpdateDuration.NewTimer())
        {

            Log.Information($"Performing account update for user {currentUser.Id} with request {accessRequest.Id}");

            var userModificationResponse = new UserModificationEvent
            {
                partId = currentUser.Key,
                eventType = UserModificationEvent.UserEvent.Modify,
                eventTime = DateTime.Now,
                accessRequestId = accessRequest.AccessRequestId,
                successful = true
            };

            if (!currentUser.IsActive)
            {
                Log.Logger.Information($"Enabling disabled user {currentUser.Id}");
                var enabled = await this.EnableOrDisableAccount(currentUser.Id, true);
                if (enabled)
                {
                    Log.Logger.Information($"User {currentUser.Id} successfully enabled");
                }
                else
                {
                    Log.Logger.Warning($"User {currentUser.Id} was not enabled");

                }
            }

            // check user has folio and is associated
            var groups = await this.GetUserOUGroups(currentUser.Id);

            var inGroup = groups.FirstOrDefault(group => group.Name.Equals(CounselGroup, StringComparison.OrdinalIgnoreCase));

            if (inGroup == null)
            {
                Log.Information($"User {currentUser.Id} not currently in {CounselGroup} - adding");
                var addedToGroup = await this.AddUserToOUGroup(currentUser.Id, CounselGroup);

                if (addedToGroup)
                {
                    Log.Information($"Added user {currentUser.Id} to group {CounselGroup}");
                }
                else
                {
                    Log.Warning($"Failed to add user {currentUser.Id} to group {CounselGroup}");
                    userModificationResponse.successful = false;
                    userModificationResponse.Errors.Add($"Failed to add user {currentUser.Id} to group {CounselGroup}");
                }
            }


            //var addedToFolio = await this.AddUserToFolio(accessRequest, currentUser);
            //if (!addedToFolio)
            //{
            //    userModificationResponse.successful = false;
            //    userModificationResponse.Errors.Add($"Failed to add user {currentUser.Id} to folio {accessRequest.FolioId}");
            //}

            return userModificationResponse;
        }

    }

    //private async Task<bool> AddUserToFolio(EdtDisclosureUserProvisioningModel accessRequest, EdtUserDto currentUser)
    //{
    //    if (currentUser == null)
    //    {
    //        Log.Error($"Null user sent to AddUserToFolio({accessRequest.Id})");
    //        return false;
    //    }

    //    // lets confirm that the caseID and UniqueID match and nobody tried to fake it
    //    var caseById = await this.GetCase(accessRequest.FolioCaseId);
    //    var folioCase = await this.FindCase("Folio ID", accessRequest.FolioId);

    //    if (caseById.Id != folioCase.Id)
    //    {
    //        Log.Error($"Invalid attempt to access case - folio and case Id do not match - possible attempt to access unauthorized cases");
    //        return false;
    //    }

    //    var existingCaseAccess = await this.GetUserCaseAccess(currentUser.Id);
    //    var alreadyAssigned = existingCaseAccess.FirstOrDefault(cases => cases.Id == folioCase.Id);

    //    if (alreadyAssigned != null)
    //    {
    //        Log.Logger.Information($"User already assigned {currentUser.Id} to folio case {accessRequest.FolioId}");
    //        var userCaseGroups = await this.GetUserCaseGroups(currentUser.Id, caseById.Id);
    //        var caseUserGroup = userCaseGroups.FirstOrDefault(group => group.GroupName.Equals(CounselGroup, StringComparison.OrdinalIgnoreCase));
    //        if ( caseUserGroup== null)
    //        {
    //            await this.AddUserToCaseGroup(currentUser.Id, caseById.Id, CounselGroup);
    //        }
    //        return true;
    //    }

    //    Log.Logger.Information($"Adding user {currentUser.Id} to folio case {accessRequest.FolioId}");
    //    var addedToCase = await this.AddUserToCase(currentUser.Id, folioCase.Id, "Case Manager");


    //    if (!addedToCase)
    //    {
    //        Log.Error($"Failed to add user {currentUser.Id} to case {folioCase.Id}");
    //        return false;
    //    }
    //    else
    //    {

    //        // add user to case group
    //        var addedToCaseGroup = await this.AddUserToCaseGroup(currentUser.Id, folioCase.Id, await this.GetCaseGroupId(folioCase.Id, CounselGroup));
    //        if (!addedToCaseGroup)
    //        {
    //            Log.Error($"Failed to add user {currentUser.Id} to case group {folioCase.Id} [{CounselGroup}]");
    //        }

    //        return addedToCaseGroup;
    //    }
    //}


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


    private async Task<IEnumerable<OrgUnitModel>> GetUserCaseAccess(string userId)
    {

        var result = await this.GetAsync<IEnumerable<OrgUnitModel>>($"api/v1/org-units/1/users/{userId}/cases");
        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            throw new EdtDisclosureServiceException($"Unable to get case access list for user {userId}");
        }
    }

    public async Task<bool> AddUserToCase(string userKey, int caseId)
    {
        var caseListing = await this.GetUserCases(userKey);

        var userOnCase = caseListing.FirstOrDefault(caze => caze.Id == caseId);

        if (userOnCase != null)
        {
            Log.Logger.Information("User user {0} already assigned to case {0}", userKey, caseId);
            return true;
        }
        else
        {
            Log.Logger.Information("Adding user {0} to case {0}", userKey, caseId);
            var result = await this.PostAsync<EdtCaseUserDto>($"api/v1/cases/{caseId}/users/{userKey}");
            if (result.IsSuccess)
            {
                Log.Information("Successfully added user {0} to case {1}", userKey, caseId);
                return true;
            }
            else
            {
                Log.Error("Failed to add user {0} to case {1} [{2}]", userKey, caseId, string.Join(',', result.Errors));
                throw new EdtDisclosureServiceException($"Failed to add user {userKey} to case {caseId} [{string.Join(',', result.Errors)}]");
            }
        }
    }

    public async Task<bool> AddUserToCase(string userKey, int caseId, int caseGroupId)
    {
        var addResponse = await this.AddUserToCase(userKey, caseId);


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
                Log.Error($"Failed to add user {userKey} to case group {caseGroupId} not assigned to case {caseId}");
                Log.Warning("************ NEED TO DETERMINE CASE GROUP ACCESS ************************");
                // throw new EdtDisclosureServiceException($"Failed to add user {userKey} to case group {CounselGroup} not assigned to case {caseId}");
            }
            else
            {
                Log.Information($"User {userKey} successfully added to group {caseGroupId} for case {caseId}");
            }

        }

        return true;
    }

    public async Task<bool> AddUserToCase(string userKey, int caseId, string caseGroupName)
    {


        var caseGroupId = await this.GetCaseGroupId(caseId, caseGroupName);

        // case group Ids are zero-based
        if (caseGroupId > -1)
        {
            return await this.AddUserToCase(userKey, caseId, caseGroupId);
        }
        else
        {
            Log.Error($"{caseGroupName} not assigned to case {caseId}");
            throw new EdtDisclosureServiceException($"{caseGroupName} not assigned to case {caseId}");
        }


        return false;
    }


    private async Task<IEnumerable<UserCaseGroup>> GetUserCaseGroups(string userKey, int caseId)
    {
        var result = await this.GetAsync<IEnumerable<UserCaseGroup>>($"api/v1/cases/{caseId}/users/{userKey}/groups");
        Log.Logger.Information("Got #{0} user cases  user {1}", result.Value.Count(), userKey);

        if (result.IsSuccess)
        {
            return result.Value;
        }
        else
        {
            throw new EdtDisclosureServiceException($"Unable to get user case groups for user {userKey} and case {caseId}");
        }

    }


    public async Task<bool> AddUserToCaseGroup(string userId, int caseId, string caseGroup)
    {
        var caseGroupId = await this.GetCaseGroupId(caseId, caseGroup);
        if (caseGroupId > 0)
        {
            return await this.AddUserToCaseGroup(userId, caseGroupId, caseGroupId);
        }
        else
        {
            Log.Warning($"No case group found for user {userId} in case group {caseGroup}");
            return false;
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
        var result = await this.DeleteAsync($"api/v1/cases/{caseId}/users/remove/{userId}");

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

        var caseIds = await this.SearchForCase(searchString);

        if (caseIds != null)
        {

            // see if we have multiple cases with the same id - if e do then we want the one with a key
            int caseId;
            if (caseIds?.Count() > 1)
            {
                Log.Information("Multiple cases found for {0}", searchString.ToString());
                caseId = caseIds.FirstOrDefault(c => c.Key != null).Id;
            }
            else
            {
                caseId = caseIds.First().Id;
            }

            return await this.GetCase(caseId);

        }
        else
        {
            Log.Information("No cases found for {0}", searchString);
            return null;
        }

    }

    private async Task<IEnumerable<CaseId>> SearchForCase(string search)
    {
        Log.Logger.Information("Finding case {0}", search);

        var result = await this.GetAsync<IEnumerable<CaseId>?>($"api/v1/org-units/1/cases/{search}/id");

        if (result.IsSuccess)
        {
            return result.Value;

        }
        else
        {
            Log.Logger.Warning("No matching cases found for {0}", search);
            return null;
        }
    }

    /// <summary>
    /// Create a new case
    /// </summary>
    /// <param name="caseInfo"></param>
    /// <returns></returns>
    /// <exception cref="EdtDisclosureServiceException"></exception>
    public async Task<int> CreateCase(EdtCaseDto caseInfo)
    {
        Log.Logger.Information($"Case creation request {caseInfo.Name}");
        if (caseInfo.Key == null || caseInfo.Name == null)
        {
            throw new EdtDisclosureServiceException($"Invalid case creation request received");
        }
        var response = await this.PostAsync<CaseModel>($"api/v1/org-units/1/cases", caseInfo);
        if (!response.IsSuccess)
        {
            var msg = $"Case creation failed {caseInfo.Name} {string.Join(",", response.Errors)}";
            Log.Logger.Error(msg);
            throw new EdtDisclosureServiceException(msg);

        }
        else
        {
            Log.Logger.Information($"Case creation complete {caseInfo.Name} {response.Value.Id}");
            return response.Value.Id;
        }


    }

    public async Task<IEnumerable<KeyIdPair>> GetUserCases(string userIdOrKey)
    {
        Log.Logger.Information($"Getting cases for user {userIdOrKey}");

        var response = await this.GetAsync<IEnumerable<KeyIdPair>>($"api/v1/org-units/1/users/{userIdOrKey}/cases");

        if (response.IsSuccess)
        {
            Log.Logger.Information($"Found {response.Value.Count()} cases for user {userIdOrKey}");
            return response.Value;
        }
        else
        {
            var msg = $"Case lookup for user {userIdOrKey} failed - {string.Join(",", response.Errors)}";
            Log.Logger.Error(msg);
            throw new EdtDisclosureServiceException(msg);
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

    // include fields by default
    public async Task<CaseModel> GetCase(int caseID) => await this.GetCase(caseID, true);


    public async Task<CaseModel> GetCase(int caseID, bool includeFields)
    {
        var result = await this.GetAsync<CaseModel?>($"api/v1/cases/{caseID}");
        if (result.IsSuccess)
        {
            if (result != null)
            {
                if (includeFields)
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
                }
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

    private async Task<IEnumerable<OrgUnitModel>> GetUserOUGroups(string userId)
    {
        var result = await this.GetAsync<IEnumerable<OrgUnitModel?>>($"api/v1/org-units/1/users/{userId}/groups");

        if (!result.IsSuccess)
        {
            Log.Warning($"Failed to get current groups for user {userId}");
            return null; //invalid
        }
        else
        {
            return result.Value;
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

            // see if user already in group


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

    public async Task<CaseModel> FindCaseByKey(string caseKey) => await this.FindCaseByKey(caseKey, true);


    /// <summary>
    ///
    /// Key is unique within cases
    /// </summary>
    /// <param name="caseKey"></param>
    /// <returns></returns>
    public async Task<CaseModel> FindCaseByKey(string caseKey, bool includeFields)
    {
        if (caseKey == null)
        {
            Log.Error($"Null key passed to FindCaseByKey()");
            return null;
        }

        var caseIds = await this.SearchForCase(caseKey);
        if (caseIds != null && caseIds.Any())
        {
            return await this.GetCase(caseIds.First().Id, includeFields);
        }
        else
        {
            Log.Information("No cases found for key {0}", caseKey);
            return null;
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
