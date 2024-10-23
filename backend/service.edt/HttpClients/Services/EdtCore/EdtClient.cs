namespace edt.service.HttpClients.Services.EdtCore;

using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using AutoMapper;
using Common.Models.EDT;
using edt.service.Exceptions;
using edt.service.Infrastructure.Telemetry;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.PersonCreationHandler.Models;
using edt.service.ServiceEvents.UserAccountModification.Models;
using Prometheus;
using Serilog;

public class EdtClient : BaseClient, IEdtClient
{
    private const string SUBMITTING_AGENCY_GROUP_NAME = "Submitting Agency";
    private readonly IMapper mapper;
    private readonly EdtServiceConfiguration configuration;
    private readonly string SUBMITTING_AGENCY = "SubmittingAgency";
    private static readonly Histogram AccountCreationDuration = Metrics.CreateHistogram("edt_account_creation_duration", "Histogram of edt account creations.");
    private static readonly Histogram AccountUpdateDuration = Metrics.CreateHistogram("edt_account_update_duration", "Histogram of edt account updates.");
    private static readonly Histogram GetUserDuration = Metrics.CreateHistogram("edt_get_user_duration", "Histogram of edt account lookups.");
    private static readonly Histogram ParticipantCreationDuration = Metrics.CreateHistogram("edt_participant_creation_duration", "Histogram of edt participant creations.");
    private static readonly Histogram ParticipantModificationDuration = Metrics.CreateHistogram("edt_participant_modification_duration", "Histogram of edt participant modifications.");
    private static readonly Histogram DisclosureLinkageDuration = Metrics.CreateHistogram("edt_person_disclosure_linkage", "Histogram of edt participant to folio linkage events.");
    private static readonly Histogram PersonSearchDuration = Metrics.CreateHistogram("edt_person_saerch_duration", "Histogram of edt person searches.");

    private readonly Counter<long> getPersonCounter;
    private readonly Counter<long> addPersonCounter;
    private readonly Counter<long> getUserCounter;
    private readonly Counter<long> updateUserCounter;
    private readonly Counter<long> updatePersonCounter;

    public EdtClient(
        HttpClient httpClient, Instrumentation instrumentation, EdtServiceConfiguration edtServiceConfiguration,
        IMapper mapper,
        ILogger<EdtClient> logger)
        : base(httpClient, logger)
    {
        ArgumentNullException.ThrowIfNull(instrumentation);
        this.mapper = mapper;
        this.configuration = edtServiceConfiguration;
        this.getPersonCounter = instrumentation.EdtGetPersonCounter;
        this.addPersonCounter = instrumentation.EdtAddPersonCounter;
        this.getUserCounter = instrumentation.EdtGetUserCounter;
        this.updateUserCounter = instrumentation.EdtUpdateUserCounter;
        this.updatePersonCounter = instrumentation.EdtUpdatePersonCounter;

    }

    public async Task<UserModificationEvent> CreateUser(EdtUserProvisioningModel accessRequest)
    {
        using (AccountCreationDuration.NewTimer())
        {
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
        var additionalGroupConfig = this.configuration.EdtClient.AdditionalBCPSGroups.Split(",", StringSplitOptions.TrimEntries);

        Log.Information($"User {userIdOrKey} is assigned to {currentlyAssignedGroups.Count} groups");
        foreach (var currentAssignedGroup in currentlyAssignedGroups)
        {
            var assignedRegion = assignedRegions.Find(region => region.RegionName.Equals(currentAssignedGroup.Name))!;
            if (assignedRegion == null && !additionalGroupConfig.Contains(assignedRegion.RegionName))
            {
                Log.Logger.Information("User {0} is in group {1} that is no longer valid", userIdOrKey, currentAssignedGroup.Name);
                var result = await this.RemoveUserFromGroup(userIdOrKey, currentAssignedGroup);
                if (!result)
                {
                    Log.Warning($"Failed to remove user {userIdOrKey} from group {currentAssignedGroup.Name}");
                    return false;
                }
            }
        }

        if (assignedRegions.Count == 0)
        {
            Log.Warning($"User {userIdOrKey} is not assigned to any regions");
        }
        else
        {

            var regions = assignedRegions.DistinctBy(row => row.RegionName);

            Log.Information($"Adding user {userIdOrKey} to {regions.Count()} regions");

            if (!regions.Any())
            {
                Log.Warning($"User {userIdOrKey} has no assigned regions");
            }

            foreach (var region in regions)
            {

                Log.Information($"Handling group {region.RegionName} for user {userIdOrKey}");
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
                        Log.Logger.Error("Region not found {0}", region.RegionName);
                        return false;
                    }

                    var result = await this.PostAsync($"api/v1/org-units/1/groups/{groupId}/users", new AddUserToOuGroup() { UserIdOrKey = userIdOrKey });

                    if (!result.IsSuccess)
                    {
                        var errorString = string.Join(", ", result.Errors);
                        if (errorString.Contains("already a member"))
                        {
                            Log.Logger.Information($"User {0} already in region {1}", userIdOrKey, region.RegionName);
                        }
                        else
                        {
                            Log.Logger.Error("Failed to add user {0} to region {1} due to {2}", userIdOrKey, region, errorString);
                            return false;
                        }
                    }
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

    private async Task<bool> AddUserToOUGroupById(string userId, int groupId)
    {
        if (groupId < 0)
        {
            throw new EdtServiceException($"Invalid groups Id {groupId} in call to AddUserToOUGroupById for user {userId}");
        }
        var result = await this.PostAsync($"api/v1/org-units/1/groups/{groupId}/users", new AddUserToOuGroup() { UserIdOrKey = userId });
        if (!result.IsSuccess)
        {
            Log.Logger.Error("Failed to add user {0} to group {1} due to {2}", userId, groupId, string.Join(",", result.Errors));
            return false;
        }
        else
        {
            Log.Logger.Information("Successfully added user {0} to region {1}", userId, groupId);
            return true;
        }
    }


    public async Task<List<EdtPersonDto>> GetPersonsByIdentifier(string identifierType, string identifierValue)
    {
        if (string.IsNullOrEmpty(identifierValue) || string.IsNullOrEmpty(identifierType))
        {
            throw new EdtServiceException("Invalid request to GetPersonsByIdentifier");
        }

        var returnPersons = new List<EdtPersonDto>();

        var result = await this.GetAsync<IdentifierResponseModel>($"api/v1/org-units/1/identifiers?filter=IdentifierValue:{identifierValue},IdentifierType:{identifierType},ItemType:Person");

        if (result.IsSuccess)
        {
            Log.Information($"Found {result.Value.Total} persons for {identifierValue}");
            foreach (var identityInfo in result.Value.Items)
            {
                returnPersons.Add(await this.GetPersonById(identityInfo.EntityId));
            }
        }

        return returnPersons;
    }

    public async Task<EdtPersonDto> GetPersonByIdentifier(string identifierType, string identifierValue)
    {
        var persons = await this.GetPersonsByIdentifier(identifierType, identifierValue);
        if (persons.Count == 1)
        {
            return persons.First();
        }
        return null;
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



    /// <summary>
    /// Add an identifier to a person
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="identifierType"></param>
    /// <param name="identifierValue"></param>
    /// <returns></returns>
    public async Task<int> AddPersonIdentifier(int personId, string identifierType, string identifierValue)
    {
        if (personId <= 0 || string.IsNullOrEmpty(identifierValue))
        {
            Log.Logger.Error($"Unable to call identifiers update for person {personId} value {identifierValue} is invalid");
            return -1;
        }

        var result = await this.PostAsync<IdentifierModel>($"api/v1/org-units/1/identifiers", new IdentifierCreationInputModel
        {
            EntityId = personId,
            IdentifierType = identifierType,
            IdentifierValue = identifierValue
        });

        if (result.IsSuccess)
        {
            Log.Logger.Information($"New person {personId} identifier added with value {identifierType} : {identifierValue} = {result.Value.Id}");
            return result.Value.Id;
        }
        else
        {
            Log.Error($"Failed to added new identifier value for {personId} with value {identifierType} : {identifierValue}");
            return -1;
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

        Log.Logger.Information("Updating EDT tombstone user {0} {1}", accessRequest.ToString(), userDetails.ToString());

        userDetails.Email = accessRequest.Email;
        userDetails.IsActive = true;

        return await this.UpdateUser(accessRequest, userDetails, true);
    }





    public async Task<UserModificationEvent> UpdateUser(EdtUserProvisioningModel accessRequest, EdtUserDto previousRequest, bool fromTombstone)
    {
        using (AccountUpdateDuration.NewTimer())
        {
            Log.Logger.Information("Updating EDT User {0} {1} TS: {2}", accessRequest.ToString(), previousRequest.ToString(), fromTombstone);
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
                Log.Error($"Failed to update EDT user with PUT request {edtUserDto.Key} {string.Join(",", result.Errors)}");
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
            this.getUserCounter.Add(1);
            Log.Logger.Information("Checking if user key {0} already present", userKey);
            var result = await this.GetAsync<EdtUserDto?>($"api/v1/users/key:{userKey}");

            if (!result.IsSuccess)
            {
                return null;
            }
            return result.Value;
        }
    }

    public async Task<EdtPersonDto?> GetPerson(string userKey)
    {
        using (GetUserDuration.NewTimer())
        {
            this.getPersonCounter.Add(1);
            Log.Logger.Information("Checking if person with key {0} already present", userKey);
            var result = await this.GetAsync<EdtPersonDto?>($"api/v1/org-units/1/persons/{userKey}");

            if (!result.IsSuccess)
            {
                return null;
            }
            var person = result.Value;
            // get identifiers
            if (person != null && person.Id > 0)
            {
                person.Identifiers = await this.GetPersonIdentifiers(person.Id);
            }
            return person;
        }
    }

    public async Task<EdtPersonDto?> GetPersonById(int id)
    {
        using (GetUserDuration.NewTimer())
        {
            this.getPersonCounter.Add(1);
            Log.Logger.Information($"Getting person by id {id}");
            var result = await this.GetAsync<EdtPersonDto?>($"api/v1/org-units/1/persons/{id}");

            if (!result.IsSuccess)
            {
                return null;
            }

            var person = result.Value;
            // get identifiers
            if (person != null && person.Id > 0)
            {
                var identifiers = await this.GetPersonIdentifiers(person.Id);
                if (identifiers != null)
                {
                    person.Identifiers = identifiers;
                }

            }
            return person;


        }
    }

    private async Task<List<IdentifierModel>> GetPersonIdentifiers(int? personID)
    {

        if (personID <= 0)
        {
            Log.Error("Invalid call to GetPersonIdentifiers");
        }

        Log.Logger.Information($"Getting identifiers for person {personID}");
        var result = await this.GetAsync<IdentifierResponseModel>($"api/v1/org-units/1/identifiers?filter=itemType:Person,itemId:{personID}");

        if (result.IsSuccess)
        {
            return result.Value.Items;
        }
        else
        {
            Log.Error($"Failed to get person identifiers for {personID}");
            return new List<IdentifierModel>();
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
        var result = await this.GetAsync<List<EdtUserGroup>?>($"api/v1/org-units/1/users/{userKey}/groups");
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

        var user = await this.GetUser(userIdOrKey);

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

    /// <summary>
    /// Remove a user from a given group (Region/Agency etc)
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public async Task<bool> RemoveUserFromGroup(string userIdOrKey, EdtUserGroup group)
    {
        Log.Logger.Information("Removing user {0} from group {1}", userIdOrKey, group.Name);
        var result = await this.DeleteAsync($"api/v1/org-units/1/groups/{group.Id}/users/{userIdOrKey}");
        if (!result.IsSuccess)
        {
            Log.Logger.Error("Failed to remove user {0} from group {1} [{2}]", userIdOrKey, group.Name, string.Join(',', result.Errors));
            return false;
        }
        else
        {
            Log.Logger.Information($"Removed user {userIdOrKey} from group {group}");
        }

        return true;
    }

    /// <summary>
    /// Add to group by ID
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public async Task<bool> AddUserToGroup(string userIdOrKey, int groupId) => await this.AddUserToOUGroupById(userIdOrKey, groupId);

    /// <summary>
    /// Get the current EDT version
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EdtServiceException"></exception>
    public async Task<string> GetVersion()
    {
        var result = await this.GetAsync<Common.Models.EDT.EdtVersion?>($"api/v1/version");

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
            this.addPersonCounter.Add(1);
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
                var newParticipant = await this.GetPerson(edtPersonDto.Key!);

                Log.Logger.Information($"Successfully added {accessRequest.LastName} as a participant");
                // add external Id
                var createdIdentifer = await this.AddPersonIdentifier((int)newParticipant.Id, this.configuration.EdtClient.DefenceParticipantAdditionalId, edtPersonDto.Key!);

            }


            return userModificationResponse;

        }
    }


    /// <summary>
    /// Permits changing a persons email
    /// </summary>
    /// <param name="modificationInfo"></param>
    /// <returns></returns>
    /// <exception cref="EdtServiceException"></exception>
    public async Task<UserModificationEvent> ModifyPerson(IncomingUserModification modificationInfo)
    {
        using (ParticipantModificationDuration.NewTimer())
        {
            this.updatePersonCounter.Add(1);

            var hasChange = false;
            var person = await this.GetPerson(modificationInfo.Key);

            if (person == null)
            {
                var msg = $"Request to update unknown user {modificationInfo.Key}";
                Log.Error(msg);
                throw new EdtServiceException(msg);
            }

            foreach (var changeEvent in modificationInfo.SingleChangeTypes)
            {
                var changeValue = changeEvent.Value;

                if (changeValue != null)
                {
                    switch (changeEvent.Key)
                    {
                        case ChangeType.EMAIL:
                        {
                            person.Address.Email = changeValue.To;
                            Log.Information($"Email address change for {modificationInfo.ChangeId} to {changeValue.To}");
                            hasChange = true;
                            break;
                        }
                        case ChangeType.PHONE:
                        {
                            person.Address.Phone = changeValue.To;
                            Log.Information($"Phone change for {modificationInfo.ChangeId} to {changeValue.To}");
                            hasChange = true;
                            break;
                        }

                    }
                }
                else
                {
                    throw new EdtServiceException($"Unable to update user {modificationInfo.Key} with invalid {changeEvent.Key} info");

                }
            }

            var userModificationResponse = new UserModificationEvent
            {
                partId = modificationInfo.Key,
                eventType = UserModificationEvent.UserEvent.Modify,
                eventTime = DateTime.Now,
                accessRequestId = modificationInfo.ChangeId
            };

            if (hasChange)
            {
                var result = await this.PutAsync($"api/v1/org-units/1/persons/" + person.Id, person);
                userModificationResponse.successful = true;

                if (!result.IsSuccess)
                {
                    Log.Logger.Error("Failed to update EDT person {0}", string.Join(",", result.Errors));
                    userModificationResponse.successful = false;
                }
            }
            else
            {
                Log.Information($"No valid changes in modification request {modificationInfo.ChangeId} - request ignored");
                userModificationResponse.successful = true;

            }


            return userModificationResponse;
        }
    }


    public async Task<UserModificationEvent> ModifyPerson(EdtPersonProvisioningModel accessRequest, EdtPersonDto currentUser)
    {
        using (ParticipantModificationDuration.NewTimer())
        {
            this.updatePersonCounter.Add(1);

            var edtPersonDto = this.mapper.Map<EdtPersonProvisioningModel, EdtPersonDto>(accessRequest);

            edtPersonDto.Id = currentUser.Id;

            edtPersonDto.Address.Id = currentUser.Address.Id;

            var result = await this.PutAsync($"api/v1/org-units/1/persons/" + currentUser.Id, edtPersonDto);
            var userModificationResponse = new UserModificationEvent
            {
                partId = accessRequest.Key,
                eventType = UserModificationEvent.UserEvent.Modify,
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


    /// <summary>
    /// Link a person to their disclosure folio in the disclosure portal
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<bool> LinkPersonToDisclosureFolio(PersonFolioLinkage request)
    {
        using (DisclosureLinkageDuration.NewTimer())
        {
            var response = false;
            Log.Logger.Information($"Linking {request.DisclosureCaseIdentifier} to {request.PersonKey}");

            // get the person by key
            var person = (request.PersonType == "Counsel") ? await this.GetPerson(request.PersonKey) : await this.GetPersonByIdentifier("edtExternalid", request.EdtExternalId);


            if (person != null)
            {
                // check no existing disc person identifier
                var existingIdentifier = person.Identifiers.FirstOrDefault(identifier => identifier.IdentifierType == "DisclosurePortalCase");
                if (existingIdentifier != null)
                {
                    Log.Information($"Found existing DisclosurePortalCase {existingIdentifier.IdentifierValue} for person {request.PersonKey}");
                    response = true;
                }
                else
                {
                    var added = await this.AddPersonIdentifier((int)person.Id, "DisclosurePortalCase", request.DisclosureCaseIdentifier);
                    if (added > 0)
                    {
                        Log.Information($"Added new person identifier {added} {person.Id} {request.PersonKey} {request.DisclosureCaseIdentifier}");
                        response = true;
                    }
                }
            }
            else
            {
                Log.Information($"Person [{request.PersonKey} was not found for folio [{request.DisclosureCaseIdentifier}] linkage - will attempt later");
            }

            return response;
        }
    }

    /// <summary>
    /// Search for a person
    /// </summary>
    /// <param name="personLookup">The person lookup model containing search criteria</param>
    /// <returns>A list of EdtPersonDto objects matching the search criteria</returns>
    public async Task<List<EdtPersonDto>> SearchForPerson(PersonLookupModel personLookup)
    {
        using (PersonSearchDuration.NewTimer())
        {
            Logger.LogInformation($"Searching for person {personLookup.LastName} {personLookup.FirstName} {personLookup.DateOfBirth}");

            // construct the filter
            List<string> filterParams = [];


            if (!string.IsNullOrEmpty(personLookup.LastName))
            {
                filterParams.Add($"LastName:{personLookup.LastName}");
            }
            if (!string.IsNullOrEmpty(personLookup.FirstName))
            {
                filterParams.Add($"FirstName:{personLookup.FirstName}");
            }
            if (personLookup.DateOfBirth != null)
            {
                filterParams.Add($"Date of Birth:{personLookup.DateOfBirth}");
            }


            var filter = string.Join(",", filterParams);

            Logger.LogInformation($"Searching filter {filter}");

            var result = await this.GetAsync<PagedItemsResponse<EdtPersonDto>>($"api/v2/org-units/1/persons?IncludeInactive={personLookup.IncludeInactive}&filter={filter}");

            if (result.IsSuccess)
            {
                if (result.Value.Total == 0)
                {
                    Logger.LogInformation($"No person found for {personLookup.LastName} {personLookup.FirstName} {personLookup.DateOfBirth}");
                    return [];
                }
                return result.Value.Items;
            }
            else
            {
                Logger.LogError($"Failed to search for person {personLookup.LastName} {personLookup.FirstName} {personLookup.DateOfBirth}");
                return [];
            }
        }
    }

    public async Task<List<UserCaseSearchResponseModel>> GetUserCases(string userKey)
    {
        var result = await this.GetAsync<List<UserCaseSearchResponseModel>?>($"api/v1/org-units/1/users/{userKey}/cases");

        if (!result.IsSuccess)
        {
            throw new EdtServiceException(string.Join(",", result.Errors));
        }

        return result.Value;
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
