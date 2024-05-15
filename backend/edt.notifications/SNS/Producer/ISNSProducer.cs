namespace edt.notifications.SNS.Producer;

using Amazon.SimpleNotificationService.Model;
using edt.notifications.Models;

public interface ISNSProducer
{
    Task<PublishResponse> ProduceAsync(EventModel eventModel);
    Task<IEnumerable<string>> ListAllTopicsAsync();
}
