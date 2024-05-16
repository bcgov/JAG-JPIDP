namespace MessagingAdapter.AWS.Producer;

using Amazon.SimpleNotificationService.Model;
using MessagingAdapter.Models;

public interface ISNSProducer
{
    Task<PublishResponse> ProduceAsync(EventModel eventModel);
    Task<IEnumerable<string>> ListAllTopicsAsync();
}
