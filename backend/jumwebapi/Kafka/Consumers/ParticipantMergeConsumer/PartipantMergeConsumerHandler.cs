namespace jumwebapi.Kafka.Consumers.ParticipantMergeConsumer;

using System.Threading.Tasks;
using global::Common.Kafka;
using jumwebapi.Data;
using jumwebapi.Models;

public class PartipantMergeConsumerHandler(ILogger<PartipantMergeConsumerHandler> logger, JumDbContext context) : IKafkaHandler<string, ParticipantMergeEvent>
{
    public async Task<Task> HandleAsync(string consumerName, string key, ParticipantMergeEvent value)

    {
        logger.LogInformation($"Message received on participant merge topic {value.SourceParticipantId} {value.TargetParticipantId}");


        // check the merge isnt already stored...


        // store the merge info
        //context.ParticipantMerges.Add()

        return Task.CompletedTask;

    }
}
