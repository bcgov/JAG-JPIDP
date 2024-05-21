namespace MessagingAdapter.AWS.Subscriber;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using Amazon.SQS.Model;
using MessagingAdapter.Configuration;
using MessagingAdapter.Data;
using MessagingAdapter.Models;
using Newtonsoft.Json.Linq;

/// <summary>
/// Subscribes to SQS Message Topics and can receive and process messages
/// </summary>
public class SQSSubscriber : ISQSSubscriber, IDisposable
{
    private readonly int maxMessages = 1;
    private readonly ILogger<SQSSubscriber> logger;
    private readonly AmazonSQSClient? client;
    private readonly IConfiguration configuration;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private MessagingAdapterContext dbContext;

    public SQSSubscriber()
    {
    }

    public SQSSubscriber(ILogger<SQSSubscriber> logger, IConfiguration configuration, MessagingAdapterContext dbContext)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.dbContext = dbContext;

        var options = this.configuration.GetAWSOptions();
        var credentialProfileStoreChain = new CredentialProfileStoreChain();

        // get AWS Credentials
        if (credentialProfileStoreChain.TryGetAWSCredentials(options.Profile, out var credentials))
        {
            options.Credentials = credentials;
        }

        // create new AWS SQS Client
        this.client = new AmazonSQSClient(options.Credentials, RegionEndpoint.CACentral1);


        this.configuration = configuration;

        // treat enums as strings not numbers
        this.jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Get messages from AWS SQS Topic
    /// </summary>
    /// <param name="qUrl"></param>
    /// <param name="waitTime"></param>
    /// <returns></returns>
    public async Task<List<DisclosureEventModel>> GetMessages()
    {
        var messages = new List<DisclosureEventModel>();
        var receiptHandles = new Dictionary<string, string>();
        var waitTime = 2;


        var subscriberOptions = new SubscriberOptions();
        this.configuration.GetSection(SubscriberOptions.Subscriber).Bind(subscriberOptions);

        var qUrl = subscriberOptions.SQSUrl;

        this.logger.LogInformation($"Getting messages from {qUrl}");

        try
        {

            var reponse = await this.client.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = this.maxMessages,
                WaitTimeSeconds = waitTime
                // (Could also request attributes, set visibility timeout, etc.)
            });

            this.logger.LogInformation($"Response message count {reponse.Messages.Count}");

            reponse.Messages.ForEach(msg =>
            {

                // check we havent processed this message previously
                if (dbContext.IsMessageProcessedAlready(msg.MessageId))
                {
                    this.logger.LogInformation($"Message {msg.MessageId} already processed");
                }
                else
                {

                    // get message and convert to EventModel object
                    var msgBody = msg.Body;
                    var json = JObject.Parse(msgBody);
                    var content = json["Message"].ToString();
                    var eventModel = JsonSerializer.Deserialize<DisclosureEventModel>(content, this.jsonSerializerOptions);
                    if (eventModel != null)
                    {
                        messages.Add(eventModel);
                    }

                    receiptHandles.Add(msg.MessageId, msg.ReceiptHandle);
                }
            });

            // track the messages we've received
            await this.TrackRecievedMessages(receiptHandles);

            // tell subscriber I'm done with these
            await this.AcknowledgeMessagesAsync(subscriberOptions.SQSUrl, receiptHandles);

            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to get messages {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Ensure we track messages so they are only ever processed once
    /// </summary>
    /// <param name="messageKeys"></param>
    /// <returns></returns>
    private async Task<int> TrackRecievedMessages(Dictionary<string, string> messageKeys)
    {
        var processed = 0;

        var txn = dbContext.Database.BeginTransaction();
        foreach (var messageKey in messageKeys)
        {
            this.dbContext.IdempotentConsumers.Add(new IdempotentConsumer
            {
                MessageId = messageKey.Key,
                ReceiptId = messageKey.Value,
                ProcessedUtc = DateTime.UtcNow,
            });
        }

        var changes = await dbContext.SaveChangesAsync();

        if (changes != messageKeys.Count)
        {
            this.logger.LogError($"Failed to track all changes count should be {messageKeys.Count} and we stored {changes} - rolling back");
            await txn.RollbackAsync();
        }
        else
        {
            await txn.CommitAsync();
        }

        return processed;
    }


    /// <summary>
    /// List available queues
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> ListQueuesAsync()
    {
        var request = new ListQueuesRequest
        {
            MaxResults = maxMessages
        };
        var response = await this.client.ListQueuesAsync(request);
        return response.QueueUrls;

    }

    public async Task<bool> AcknowledgeMessagesAsync(string qUrl, Dictionary<string, string> receiptHandles)
    {
        foreach (var receiptHandle in receiptHandles)
        {
            logger.LogInformation($"Removing message {receiptHandle.Key}, {receiptHandle.Value}");
            var response = await this.client.DeleteMessageAsync(qUrl, receiptHandle.Value);
            if (response != null)
            {
                this.logger.LogInformation($"Delete response {response.HttpStatusCode}");
            }
        }

        return true;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.client?.Dispose();
        }
    }
}
