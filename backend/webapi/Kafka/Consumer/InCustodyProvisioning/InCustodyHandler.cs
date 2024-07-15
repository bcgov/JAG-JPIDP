namespace Pidp.Kafka.Consumer.InCustodyProvisioning;

using Common.Models.CORNET;
using Pidp.Data;
using Pidp.Kafka.Interfaces;

/// <summary>
/// Handles incoming in-custody messages from Kafka
/// </summary>
/// <param name="logger"></param>
/// <param name="dbContext"></param>
/// <param name="inCustodyService"></param>
public class InCustodyHandler(ILogger<InCustodyHandler> logger, PidpDbContext dbContext, IInCustodyService inCustodyService) : IKafkaHandler<string, InCustodyParticipantModel>
{

    public async Task<Task> HandleAsync(string consumerName, string key, InCustodyParticipantModel value)
    {

        // check we havent consumed this message before
        var processedAlready = await dbContext.HasMessageBeenProcessed(key, consumerName);
        if (processedAlready)
        {
            logger.LogMessageAlreadyProcessed(key);
            return Task.CompletedTask;
        }

        logger.LogInCustodyMessageReceived(consumerName, key);


        // service will create the keycloak account (if not present) and then inform Disclosure service to provision account
        // and link cases to the user
        var result = await inCustodyService.ProcessInCustodySubmissionMessage(value);

        if (result.IsCompleted)
        {

            //add to tell message has been processed by consumer
            await dbContext.AddIdempotentConsumer(messageId: key, consumer: consumerName);
        }

        return result;
    }
}
public static partial class InCustodyHandlerLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "InCustody Message received on {consumerName} with key {key}")]
    public static partial void LogInCustodyMessageReceived(this ILogger logger, string consumerName, string key);

}
