namespace edt.notifications.SNS.Producer;

using System.Collections.Generic;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using edt.notifications.Configuration;
using edt.notifications.Models;

public class SNSProducer : ISNSProducer
{
    private ILogger<SNSProducer> logger;
    private AmazonSimpleNotificationServiceClient client;
    private readonly IConfiguration configuration;

    public SNSProducer()
    {
    }



    public SNSProducer(ILogger<SNSProducer> logger, IConfiguration configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
        var options = this.configuration.GetAWSOptions();
        var credentialProfileStoreChain = new CredentialProfileStoreChain();
        if (credentialProfileStoreChain.TryGetAWSCredentials(options.Profile, out var credentials))
        {
            options.Credentials = credentials;
        }

        this.client = new AmazonSimpleNotificationServiceClient(options.Credentials, RegionEndpoint.CACentral1);


    }

    public async Task<PublishResponse> ProduceAsync(EventModel eventModel)
    {
        var attributes = new Dictionary<string, MessageAttributeValue>();

        var filterType = (eventModel is DisclosureEventModel model) ? model.DisclosureEventType.ToString() : "unknown";
        var value = new MessageAttributeValue
        {
            DataType = "String",
            StringValue = filterType
        };

        attributes.Add("EventType", value);

        var publisherOptions = new PublisherOptions();
        this.configuration.GetSection(PublisherOptions.Publisher).Bind(publisherOptions);

        var publishRequest = new PublishRequest
        {
            Message = eventModel.AsJSON(),
            MessageAttributes = attributes,
            Subject = "Disclosure Test",
            TopicArn = publisherOptions.SNSTarget
        };
        return await this.client.PublishAsync(publishRequest);
    }




    /// <summary>
    /// Get all topic names
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> ListAllTopicsAsync()
    {
        var topics = await this.client.ListTopicsAsync();
        topics.Topics.ForEach(topic =>
        {
            this.logger.LogInformation($"Topic {topic.TopicArn}");
        });

        return topics.Topics.Select(t => t.TopicArn.ToString()).ToList();

    }
}
