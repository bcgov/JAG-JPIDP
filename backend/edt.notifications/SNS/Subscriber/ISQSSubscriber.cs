namespace edt.notifications.SNS.Subscriber;

using System.Threading.Tasks;
using Amazon.SQS.Model;

public interface ISQSSubscriber
{
    Task<ReceiveMessageResponse> GetMessages(string qUrl, int waitTime);
    Task<IEnumerable<string>> ListQueuesAsync();
    Task<bool> AcknowledgeMessagesAsync(string qUrl, List<string> receiptHandles);

}
