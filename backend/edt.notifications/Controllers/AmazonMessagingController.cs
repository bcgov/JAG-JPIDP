namespace edt.notifications.Controllers;

using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using edt.notifications.Configuration;
using edt.notifications.Models;
using edt.notifications.SNS.Producer;
using edt.notifications.SNS.Subscriber;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("[controller]")]
public class AmazonMessagingController : ControllerBase
{

    private readonly ILogger<AmazonMessagingController> logger;
    private IConfiguration configuration;
    private ISQSSubscriber subscriber;
    private ISNSProducer producer;
    private JsonSerializerOptions jsonSerializerOptions;

    public AmazonMessagingController(ILogger<AmazonMessagingController> logger, IConfiguration configuration, ISQSSubscriber subscriber, ISNSProducer producer)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.subscriber = subscriber;
        this.producer = producer;
        this.jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = {
                        new JsonStringEnumConverter() }
        };
    }

    public static async Task PublishToTopicAsync(
         IAmazonSimpleNotificationService client,
         string topicArn,
         string messageText)
    {
        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = messageText,
        };

        var response = await client.PublishAsync(request);

        Console.WriteLine($"Successfully published message ID: {response.MessageId}");
    }


    /// <summary>
    /// Post a message to the topic
    /// </summary>
    /// <param name="eventModel"></param>
    /// <returns></returns>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("/submit-request", Name = "PublishToSNS")]
    public async Task<PublishResponse> PublishToSNSTopic(DisclosureEventModel eventModel)
    {
        var response = await this.producer.ProduceAsync(eventModel);

        return response;
    }

    /// <summary>
    /// Get Messages from the topic - coded to pickup from settings
    /// </summary>
    /// <returns></returns>
    [HttpGet("/messages", Name = "GetMessages")]
    public async Task<IEnumerable<DisclosureEventModel>> GetMessagesAsync()
    {
        var messages = new List<DisclosureEventModel>();
        var receiptHandles = new List<string>();
        try
        {
            var subscriberOptions = new SubscriberOptions();
            this.configuration.GetSection(SubscriberOptions.Subscriber).Bind(subscriberOptions);


            var messageResponse = await this.subscriber.GetMessages(subscriberOptions.SQSUrl, 10);
            messageResponse.Messages.ForEach(msg =>
            {
                var msgBody = msg.Body;
                var json = JObject.Parse(msgBody);
                var content = json["Message"].ToString();
                var eventModel = JsonSerializer.Deserialize<DisclosureEventModel>(content, this.jsonSerializerOptions);
                messages.Add(eventModel);

                receiptHandles.Add(msg.ReceiptHandle);
            });


            // tell subscriber I'm done with these
            await this.subscriber.AcknowledgeMessagesAsync(subscriberOptions.SQSUrl, receiptHandles);

        }
        catch (Exception ex)
        {
            this.logger.LogError($"Error {ex.Message}");
        }
        return messages;
    }



    [HttpGet("/topics", Name = "GetTopics")]
    public async Task<IEnumerable<string>> GetTopicsAsync() => await this.producer.ListAllTopicsAsync();

    [HttpGet("/queues", Name = "GetQueues")]
    public async Task<IEnumerable<string>> GetQueuesAsync() => await this.subscriber.ListQueuesAsync();

}
