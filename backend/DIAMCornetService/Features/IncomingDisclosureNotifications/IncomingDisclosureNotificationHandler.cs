namespace DIAMCornetService.Features.MessageConsumer;

using System.Threading.Tasks;
using Common.Kafka;
using DIAMCornetService.Data;
using DIAMCornetService.Services;
using global::DIAMCornetService.Models;

public class IncomingDisclosureNotificationHandler : IKafkaHandler<string, IncomingDisclosureNotificationModel>
{
    private readonly ILogger<IncomingDisclosureNotificationHandler> logger;
    private ICornetService cornetService;
    private DIAMCornetDbContext context;

    public IncomingDisclosureNotificationHandler(ILogger<IncomingDisclosureNotificationHandler> logger, ICornetService cornetService, DIAMCornetDbContext context)
    {
        this.logger = logger;
        this.cornetService = cornetService;
        this.context = context;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, IncomingDisclosureNotificationModel value)
    {

        var incomingMessage = new IncomingMessage();

        try
        {
            // check we havent consumed this message before
            var processedAlready = await this.context.HasMessageBeenProcessed(key, consumerName);
            if (processedAlready)
            {
                logger.LogWarning($"Already processed message with key {key}");
                return Task.CompletedTask;
            }

            // track this message
            incomingMessage.MessageId = key;
            incomingMessage.ParticipantId = value.ParticipantId;
            incomingMessage.MessageTimestamp = DateTime.UtcNow;

            // we'll track message processing time and responses here
            this.context.Add(incomingMessage);

            logger.LogInformation("Message received on {0} with key {1}", consumerName, key);

            // this is where we'll produce a response
            var response = await this.cornetService.LookupCSNumberForParticipant(value.ParticipantId);

            if (response.ErrorType != null)
            {
                // error on getting the CS Number
                response = await this.cornetService.PublishErrorsToDIAMAsync(response);
            }
            else
            {

                incomingMessage.CSNumber = response.CSNumber;

                // submit notification to users
                response = await this.cornetService.SubmitNotificationToEServices(response, value.MessageText);

                // if submission was good we'll notify DIAM to provision the account
                if (response.ErrorType == null)
                {
                    response = await this.cornetService.PublishNotificationToDIAMAsync(response);
                }
                // otherwise we'll notify the business of the errors
                else
                {
                    response = await this.cornetService.PublishErrorsToDIAMAsync(response);
                }

            }
            //add to tell message has been processed by consumer
            await this.context.AddIdempotentConsumer(messageId: key, consumer: consumerName);

            incomingMessage.CompletedTimestamp = DateTime.UtcNow;

            return Task.CompletedTask;

        }
        catch (Exception ex)
        {
            incomingMessage.ErrorMessage = ex.Message;
            logger.LogError(ex, "Error processing message with key {0}", key);
            throw;
        }
        finally
        {
            await this.context.SaveChangesAsync();
        }
    }
}
