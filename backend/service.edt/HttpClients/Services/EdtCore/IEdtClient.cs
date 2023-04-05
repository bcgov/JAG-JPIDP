namespace edt.service.HttpClients.Services.EdtCore;

using edt.service.Kafka.Model;

public interface IEdtClient
{
    Task<UserModificationEvent> CreateUser(EdtUserProvisioningModel accessRequest);
    Task<UserModificationEvent> UpdateUser(EdtUserProvisioningModel accessRequest, EdtUserDto previousRequest);

    Task<int> GetOuGroupId(string regionName);

    Task<EdtUserDto?> GetUser(string userKey);

    /// <summary>
    /// Get the version of EDT (also acts as a simple ping test)
    /// </summary>
    /// <returns></returns>
    Task<string> GetVersion();

    /// <summary>
    /// Get EDT assigned groups for the user
    /// </summary>
    /// <param name="userKey">EDT user key</param>
    /// <returns></returns>
    Task<List<EdtUserGroup>> GetAssignedOUGroups(string userKey);

    /// <summary>
    /// Update the users groups in EDT
    /// This may involve adding new groups or removing non-assigned groups
    /// TODO - later this should be triggered from any change to the groups in JUSTIN
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <param name="assignedRegions"></param>
    /// <returns></returns>
    Task<bool> UpdateUserAssignedGroups(string userIdOrKey, List<AssignedRegion> assignedRegions, UserModificationEvent userModificationEvent);

    /// <summary>
    /// Remove the user from the group
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    Task<bool> RemoveUserFromGroup(string userIdOrKey, EdtUserGroup group);


    /// <summary>
    /// Flag account as inactive
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <returns></returns>
    Task<bool> DisableAccount(string userIdOrKey);


    /// <summary>
    /// Flag account as active
    /// </summary>
    /// <param name="userIdOrKey"></param>
    /// <returns></returns>
    Task<bool> EnableAccount(string userIdOrKey);

    /// <summary>
    /// Alternate way to update EDT groups
    /// </summary>
    /// <param name="key"></param>
    /// <param name="newRegions"></param>
    /// <param name="removedRegions"></param>
    /// <returns></returns>
    Task UpdateUserAssignedGroups(string key, List<string> newRegions, List<string> removedRegions);
}
