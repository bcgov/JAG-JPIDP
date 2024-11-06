namespace jumwebapi.Kafka.Consumers.ParticipantMergeConsumer;

using System.Threading.Tasks;
using global::Common.Kafka;
using jumwebapi.Data;
using jumwebapi.Data.ef;
using jumwebapi.Models;
using Microsoft.EntityFrameworkCore;

public class PartipantMergeConsumerHandler(ILogger<PartipantMergeConsumerHandler> logger, JumDbContext context) : IKafkaHandler<string, ParticipantMergeEvent>
{
    public async Task<Task> HandleAsync(string consumerName, string key, ParticipantMergeEvent value)

    {
        Serilog.Log.Information($"Message received on participant merge topic {value.SourceParticipantId} {value.TargetParticipantId}");

        try
        {
            // check we havent consumed this message before
            var processedAlready = await context.HasBeenProcessed(key, consumerName);
            if (processedAlready)
            {
                Serilog.Log.Information($"Already processed Participant merge with key {key}");
                return Task.CompletedTask;
            }

            //add to tell message has been processed by consumer
            await context.AddIdempotentConsumer(messageId: key, consumer: consumerName);

            // Check if the merge record already exists based on Id
            var existingMerge = await context.ParticipantMerge
                .Where(pm => pm.Id == key)
                .FirstOrDefaultAsync();

            if (existingMerge == null)
            {
                // Create a new merge entry since no existing record was found
                var newParticipantMerge = new ParticipantMerges
                {
                    Id = key,
                    MergeEventTime = DateTime.UtcNow,
                    SourceParticipantId = value.SourceParticipantId,
                    TargetParticipantId = value.TargetParticipantId
                };

                // Add the new entry to the ParticipantMerge table
                await context.ParticipantMerge.AddAsync(newParticipantMerge);

            }
            else
            {
                // handle cases where the merge entry already exists
                Serilog.Log.Information("Participant merge already exists in the database.");
            }

            return Task.CompletedTask;

        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error processing Participant merge event with key {0}", key);
            throw;
        }
        finally
        {
            // Save the changes to the database
            await context.SaveChangesAsync();
        }

    }
}
