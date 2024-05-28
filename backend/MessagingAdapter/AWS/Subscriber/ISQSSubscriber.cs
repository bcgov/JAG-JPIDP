namespace MessagingAdapter.AWS.Subscriber;

using System.Threading.Tasks;
using MessagingAdapter.Models;

public interface ISQSSubscriber
{
    Task<List<DisclosureEventModel>> GetMessages();
    Task<IEnumerable<string>> ListQueuesAsync();
    Task<bool> AcknowledgeMessagesAsync(string qUrl, Dictionary<string, string> receiptHandles);

}
