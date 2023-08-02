namespace jumwebapi.Features.UserChangeManagement.Services;

using jumwebapi.Models;

public interface IJustinUserChangeService
{

    /// <summary>
    /// Process the events and send a response
    /// </summary>
    /// <param name="events"></param>
    /// <returns></returns>
    Task<Task> ProcessChangeEvents(IEnumerable<JustinUserChangeEvent> events);
}
