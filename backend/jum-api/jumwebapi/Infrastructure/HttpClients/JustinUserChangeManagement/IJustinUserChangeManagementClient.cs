namespace jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;

using jumwebapi.Models;

/// <summary>
/// Keeping this separate as at some point we'll want to move away from ORDS but might be done in a strangler pattern approach
/// </summary>
public interface IJustinUserChangeManagementClient
{

    /// <summary>
    /// Get all current change events
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<JustinUserChangeEvent>> GetCurrentChangeEvents();

    /// <summary>
    /// Re-queue an indivual JUSTIN change event
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    Task<bool> RequeueChangeEvent(int eventId);
}
