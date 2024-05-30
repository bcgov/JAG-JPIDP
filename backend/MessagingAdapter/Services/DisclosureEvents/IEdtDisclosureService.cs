namespace MessagingAdapter.Services.DisclosureEvents;

using MessagingAdapter.Models;

/// <summary>
/// Get events from EDT Core service for disclosures
/// </summary>
public interface IEdtDisclosureService
{
    public Task<List<DisclosureEventModel>> GetDisclosureEventsAsync();
}
