namespace edt.notifications.SNS.Subscriber;

using System.Collections.Generic;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using Amazon.SQS.Model;

public class SQSSubscriber : ISQSSubscriber
{
    private int MaxMessages = 10;
    private ILogger<SQSSubscriber> logger;
    private IAmazonSQS client;
    private readonly IConfiguration configuration;

    public SQSSubscriber()
    {
    }

    public SQSSubscriber(ILogger<SQSSubscriber> logger, IConfiguration configuration)
    {
        this.MaxMessages = MaxMessages;
        this.logger = logger;
        this.configuration = configuration;
        var options = this.configuration.GetAWSOptions();
        var credentialProfileStoreChain = new CredentialProfileStoreChain();
        if (credentialProfileStoreChain.TryGetAWSCredentials(options.Profile, out var credentials))
        {
            options.Credentials = credentials;
        }
        this.client = new AmazonSQSClient(options.Credentials, RegionEndpoint.CACentral1);
        this.configuration = configuration;
    }

    public async Task<ReceiveMessageResponse> GetMessages(
     string qUrl, int waitTime = 0)
    {
        logger.LogInformation($"Getting messages from {qUrl}");

        try
        {
            var reponse = await client.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = MaxMessages,
                WaitTimeSeconds = waitTime
                // (Could also request attributes, set visibility timeout, etc.)
            });

            logger.LogInformation($"Response message count {reponse.Messages.Count}");

            return reponse;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to get messages {ex.Message}", ex);
            return null;
        }
    }



    /// <summary>
    /// List available queues
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> ListQueuesAsync()
    {
        var request = new ListQueuesRequest
        {
            MaxResults = MaxMessages
        };
        var response = await client.ListQueuesAsync(request);
        return response.QueueUrls;

    }

    public async Task<bool> AcknowledgeMessagesAsync(string qUrl, List<string> receiptHandles)
    {
        foreach (var receiptHandle in receiptHandles)
        {
            var response = await this.client.DeleteMessageAsync(qUrl, receiptHandle);
            if (response != null)
            {
                this.logger.LogInformation($"Delete response {response.HttpStatusCode}");
            }
        }

        return true;
    }
}
