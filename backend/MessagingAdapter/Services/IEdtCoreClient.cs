namespace MessagingAdapter.Services;

using MessagingAdapter.Models;

public interface IEdtCoreClient
{
    Task<List<DisclosureEventModel>> GetDisclosureEvents(DateTime fromDate);
}
