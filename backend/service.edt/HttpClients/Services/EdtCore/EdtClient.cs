namespace edt.service.HttpClients.Services.EdtCore;

using System.Threading.Tasks;
using AutoMapper;
using DomainResults.Common;
using edt.service.Exceptions;
using edt.service.Infrastructure.Telemetry;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.UserAccountCreation.Models;
using Prometheus;
using Serilog;

public class EdtClient : BaseClient, IEdtClient
{
    private const string SUBMITTING_AGENCY_GROUP_NAME = "Submitting Agency";
    private readonly IMapper mapper;
    private readonly OtelMetrics meters;
    private readonly EdtServiceConfiguration configuration;
    private readonly string SUBMITTING_AGENCY = "SubmittingAgency";
    private static readonly Histogram AccountCreationDuration = Metrics.CreateHistogram("edt_account_creation_duration", "Histogram of edt account creations.");
    private static readonly Histogram AccountUpdateDuration = Metrics.CreateHistogram("edt_account_update_duration", "Histogram of edt account updates.");
    private static readonly Histogram GetUserDuration = Metrics.CreateHistogram("edt_get_user_duration", "Histogram of edt account lookups.");
    private static readonly Histogram ParticipantCreationDuration = Metrics.CreateHistogram("edt_participant_creation_duration", "Histogram of edt participant creations.");
    private static readonly Histogram ParticipantModificationDuration = Metrics.CreateHistogram("edt_participant_modification_duration", "Histogram of edt participant modifications.");



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

    public async Task<UserModificationEvent> CreateUser(EdtUserProvisioningModel accessRequest)
    {
        using (AccountCreationDuration.NewTimer())
        {
            this.meters.AddUser();
            var edtUserDto = this.mapper.Map<EdtUserProvisioningModel, EdtUserDto>(accessRequest);
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
            var getUser = await this.GetUser(accessRequest.Key!);

            if (getUser != null)
            {
                if (accessRequest.OrganizationType != null && accessRequest.OrganizationType.Equals(this.SUBMITTING_AGENCY, StringComparison.Ordinal))
                {
                    userModificationResponse.submittingAgencyUser = true;
                    Log.Logger.Information("Adding user {0} {1} to submitting agency group", accessRequest.Id, accessRequest.Key);
                    var addGroupToUser = await this.AddUserToSubmittingAgencyGroup(accessRequest, getUser.Id);
                    if (!addGroupToUser)
                    {
                        Log.Logger.Error("Failed to add EDT user to group user {0}", string.Join(",", result.Errors));
                    }
                }
                else
                {
                    var addGroupToUser = await this.UpdateUserAssignedGroups(getUser.Id!, accessRequest.AssignedRegions!, userModificationResponse);
                    if (!addGroupToUser)
                    {
                        Log.Logger.Error("Failed to add EDT user to group user {0}", string.Join(",", result.Errors));
                    }
                }
            }
            else
            {
                userModificationResponse.successful = false;
            }
            return userModificationResponse;

        }

    }
    public async Task<bool> UpdateUserAssignedGroups(string userIdOrKey, List<AssignedRegion> assignedRegions, UserModificationEvent modificationEvent)
    {

        // Get existing groups assigned to user
        var currentlyAssignedGroups = await this.GetAssignedOUGroups(userIdOrKey);

        foreach (var currentAssignedGroup in currentlyAssignedGroups)
        {
            var assignedRegion = assignedRegions.Find(region => region.RegionName.Equals(currentAssignedGroup.Name))!;
            if (assignedRegion == null)
            {
                Log.Logger.Information("User {0} is in group {1} that is no longer valid", userIdOrKey, currentAssignedGroup.Name);
                var result = await this.RemoveUserFromGroup(userIdOrKey, currentAssignedGroup);
                if (result)
                {
                    return false;
                }
            }
        }


        foreach (var region in assignedRegions)
        {

            var existingGroup = currentlyAssignedGroups.Find(g => g.Name.Equals(region.RegionName, StringComparison.Ordinal));

            if (existingGroup != null)
            {
                Log.Logger.Information("User {0} already assigned to group {1}", userIdOrKey, existingGroup.Name);
            }
            else
            {

                Log.Logger.Information("Adding user {0} to region {1}", userIdOrKey, region);
                var groupId = await this.GetOuGroupId(region.RegionName!);
                if (groupId == 0)
                {
                    return false;
                }

                var result = await this.PostAsync($"api/v1/org-units/1/groups/{groupId}/users", new AddUserToOuGroup() { UserIdOrKey = userIdOrKey });

                if (!result.IsSuccess)
                {
                    Log.Logger.Error("Failed to add user {0} to region {1} due to {2}", userIdOrKey, region, string.Join(",", result.Errors));
                    return false;
                }
            }
        }

        Log.Logger.Information("User {0} group synchronization completed", userIdOrKey);
        return true;

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
                Log.Logger.Information("Successfully added user {0} to region {1}", userId, groupName);
                return true;
            }
        }


    }

    private async Task<bool> AddUserToSubmittingAgencyGroup(EdtUserProvisioningModel accessRequest, string userId)
    {
        if (accessRequest == null)
        {
            return false;
        }
        var groupId = await this.GetOuGroupId(SUBMITTING_AGENCY_GROUP_NAME);

        if (groupId > 0)
        {
            var result = await this.PostAsync($"api/v1/org-units/1/groups/{groupId}/users", new AddUserToOuGroup() { UserIdOrKey = userId });

            if (!result.IsSuccess)
            {
                Log.Logger.Error("Failed to add user {0} to submitting agency group due to {1}", userId, string.Join(",", result.Errors));
                return false;
            }
            else
            {
                Log.Information("User {0} added to submitting agency group", userId);
                return true;
            }
        }
        else
        {
            Log.Error("Failed to determine group Id for Submitting Agency users");
            return false;
        }


    }

    public async Task<UserModificationEvent> UpdateUserDetails(EdtUserDto userDetails)
    {
        if (userDetails == null)
        {
            throw new EdtServiceException("Null user info passed to UpdateUserDetails");
        }

        Log.Logger.Information($"Updating EDT User {userDetails}");
        var result = await this.PutAsync($"api/v1/users", userDetails);

        return new UserModificationEvent
        {
            partId = userDetails.Key,
            eventType = UserModificationEvent.UserEvent.Modify,
            eventTime = DateTime.Now,
            successful = result.IsSuccess
        };
    }

    public async Task<UserModificationEvent> EnableTombstoneAccount(EdtUserProvisioningModel accessRequest, EdtUserDto userDetails)
    {

        Log.Logger.Information("Updating EDT User {0} {1}", accessRequest.ToString(), userDetails.ToString());

        userDetails.Email = accessRequest.Email;
        userDetails.IsActive = true;

        return await this.UpdateUser(accessRequest, userDetails, true);
    }




    public async Task<UserModificationEvent> UpdateUser(EdtUserProvisioningModel accessRequest, EdtUserDto previousRequest, bool fromTombstone)
    {
        using (AccountUpdateDuration.NewTimer())
        {
            Log.Logger.Information("Updating EDT User {0} {1}", accessRequest.ToString(), previousRequest.ToString());
            var edtUserDto = this.mapper.Map<EdtUserProvisioningModel, EdtUserDto>(accessRequest);
            edtUserDto.Id = previousRequest.Id;
            var result = await this.PutAsync($"api/v1/users", edtUserDto);
            var userModificationResponse = new UserModificationEvent
            {
                partId = edtUserDto.Key,
                eventType = fromTombstone ? UserModificationEvent.UserEvent.EnableTombstone : UserModificationEvent.UserEvent.Modify,
                eventTime = DateTime.Now,
                accessRequestId = accessRequest.AccessRequestId,
                successful = result.IsSuccess
            };

            if (!result.IsSuccess)
            {
                userModificationResponse.successful = false;
            }
            //add user to group
            var user = await this.GetUser(accessRequest.Key!);
            if (user != null)
            {
                if (accessRequest.OrganizationType != null && accessRequest.OrganizationType.Equals(this.SUBMITTING_AGENCY, StringComparison.Ordinal))
                {
                    userModificationResponse.submittingAgencyUser = true;

                    // get user groups
                    var groups = await this.GetAssignedOUGroups(user.Id);
                    var alreadyMember = groups.Any(group => group.Name.Equals(SUBMITTING_AGENCY_GROUP_NAME, StringComparison.Ordinal));

                    if (alreadyMember)
                    {
                        Log.Logger.Information($"User {accessRequest.Key} already member of {SUBMITTING_AGENCY_GROUP_NAME}");
                    }
                    else
                    {
                        Log.Logger.Information($"Adding user {accessRequest.Key} to {SUBMITTING_AGENCY_GROUP_NAME}");
                        var addGroupToUser = await this.AddUserToSubmittingAgencyGroup(accessRequest, user.Id);
                        if (!addGroupToUser)
                        {
                            Log.Logger.Error($"Failed to add user {user.Id} to group {SUBMITTING_AGENCY_GROUP_NAME} {string.Join(",", result.Errors)}");
                        }
                    }
                }
                else
                {
                    var addGroupToUser = await this.UpdateUserAssignedGroups(user.Id!, accessRequest.AssignedRegions!, userModificationResponse);
                    if (!addGroupToUser)
                    {
                        userModificationResponse.successful = false;
                    }
                }
            }
            else
            {
                var msg = $"Failed to add user {accessRequest.Id} to group {accessRequest.AssignedRegions}";
                Log.Logger.Error(msg);
                userModificationResponse.successful = false;
            }


            return userModificationResponse;
        }
    }


    public async Task<EdtUserDto?> GetUser(string userKey)
    {
        using (GetUserDuration.NewTimer())
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
    }

    public async Task<EdtPersonUpdateDto?> GetPerson(string userKey)
    {
        using (GetUserDuration.NewTimer())
        {
            this.meters.GetUser();
            Log.Logger.Information("Checking if person with key {0} already present", userKey);
            var result = await this.GetAsync<EdtPersonUpdateDto?>($"api/v1/org-units/1/persons/{userKey}");

            if (!result.IsSuccess)
            {
                return null;
            }
            return result.Value;
        }
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

    public async Task<List<EdtUserGroup>> GetAssignedOUGroups(string userKey)
    {
        IDomainResult<List<EdtUserGroup>?>? result = await this.GetAsync<List<EdtUserGroup>?>($"api/v1/org-units/1/users/{userKey}/groups");
        if (!result.IsSuccess)
        {
            Log.Logger.Error("Failed to determine existing EDT groups for {0} [{1}]", string.Join(", ", result.Errors));
            return null; //invalid
        }

        return result.Value;

    }


    public async Task<bool> EnableAccount(string userIdOrKey) => await this.EnableOrDisableAccount(userIdOrKey, true);

    public async Task<bool> DisableAccount(string userIdOrKey) => await this.EnableOrDisableAccount(userIdOrKey, false);


    /// <summary>
    /// Enable or disable an account
    /// With SSO we already will disable the keycloak login but this offers another level of assurance
    /// and is an indicator in EDT that the account is inative.
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <param name="activateAccount"></param>
    /// <returns></returns>
    /// <exception cref="EdtServiceException"></exception>
    public async Task<bool> EnableOrDisableAccount(string userIdOrKey, bool activateAccount)
    {
        Log.Logger.Information($"Account change active to {activateAccount} for user {userIdOrKey}");

        EdtUserDto? user = await this.GetUser(userIdOrKey);

        if (user == null)
        {
            throw new EdtServiceException($"User {userIdOrKey} does not exist for update");
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


    public async Task<bool> RemoveUserFromGroup(string userIdOrKey, EdtUserGroup group)
    {
        Log.Logger.Information("Removing user {0} from group {1}", userIdOrKey, group.Name);
        var result = await this.DeleteAsync($"api/v1/org-units/1/groups/{group.Id}/users/{userIdOrKey}");
        if (!result.IsSuccess)
        {
            Log.Logger.Error("Failed to remove user {0} from group {1} [{2}]", userIdOrKey, group.Name, string.Join(',', result.Errors));
            return false;
        }

        return true;
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

    public async Task<bool> UpdateUserAssignedGroups(string key, List<string> newRegions, List<string> removedRegions)
    {

        Log.Information($"Updating regions for user {key}");
        var user = await this.GetUser(key);
        var changesMade = false;

        if (user == null)
        {
            Log.Error($"Failed to find EDT user with key for region update {key}");
            throw new EdtServiceException($"Failed to find EDT user with key for region update {key}");
        }
        else
        {
            var currentGroups = await this.GetAssignedOUGroups(user.Id);

            foreach (var region in newRegions)
            {
                var alreadyPresent = currentGroups.Select(g => g.Name.Equals(region, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (!alreadyPresent)
                {
                    changesMade = true;
                    var response = await this.AddUserToOUGroup(user.Id, region);
                    if (!response)
                    {
                        throw new EdtServiceException($"Failed to add user {key} to {region}");
                    }
                }
            }

            foreach (var region in removedRegions)
            {
                var existingGroup = currentGroups.Where(g => g.Name.Equals(region, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (existingGroup != null)
                {
                    changesMade = true;
                    var response = await this.RemoveUserFromGroup(user.Id, existingGroup);
                    if (!response)
                    {
                        throw new EdtServiceException($"Failed to remove user {key} from {existingGroup.Name}");
                    }

                }
            }

            if (!changesMade)
            {
                Log.Information($"No changes to existing regions were detected for {key}");
            }
        }

        return changesMade;


    }



    public async Task<UserModificationEvent> CreatePerson(EdtPersonProvisioningModel accessRequest)
    {

        using (ParticipantCreationDuration.NewTimer())
        {
            this.meters.AddPerson();
            var edtPersonDto = this.mapper.Map<EdtPersonProvisioningModel, EdtPersonDto>(accessRequest);
            var result = await this.PostAsync($"api/v1/org-units/1/persons", edtPersonDto);
            var userModificationResponse = new UserModificationEvent
            {
                partId = edtPersonDto.Key,
                eventType = UserModificationEvent.UserEvent.Create,
                eventTime = DateTime.Now,
                accessRequestId = accessRequest.AccessRequestId,
                successful = true
            };

            if (!result.IsSuccess)
            {
                Log.Logger.Error("Failed to create EDT participant {0}", string.Join(",", result.Errors));
                userModificationResponse.successful = false;
            }
            else
            {
                Log.Logger.Information($"Successfully added {accessRequest.LastName} as a participant");

            }


            return userModificationResponse;

        }
    }


    public async Task<UserModificationEvent> ModifyPerson(EdtPersonProvisioningModel accessRequest, EdtPersonUpdateDto currentUser)
    {
        using (ParticipantModificationDuration.NewTimer())
        {
            this.meters.UpdatePerson();
            var edtPersonDto = this.mapper.Map<EdtPersonProvisioningModel, EdtPersonUpdateDto>(accessRequest);
            edtPersonDto.Id = currentUser.Id;
            edtPersonDto.Address.Id = currentUser.Address.Id;
            var result = await this.PutAsync($"api/v1/org-units/1/persons/" + currentUser.Id, edtPersonDto);
            var userModificationResponse = new UserModificationEvent
            {
                partId = edtPersonDto.Key,
                eventType = UserModificationEvent.UserEvent.Create,
                eventTime = DateTime.Now,
                accessRequestId = accessRequest.AccessRequestId,
                successful = true
            };

            if (!result.IsSuccess)
            {
                Log.Logger.Error("Failed to update EDT person {0}", string.Join(",", result.Errors));
                userModificationResponse.successful = false;
            }


            return userModificationResponse;
        }
    }

    public class AddUserToOuGroup
    {
        public string? UserIdOrKey { get; set; }
    }


    public class AccountChange
    {
        public string Id { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }



}
