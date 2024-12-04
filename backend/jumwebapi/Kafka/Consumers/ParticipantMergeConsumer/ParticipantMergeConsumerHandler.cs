namespace jumwebapi.Kafka.Consumers.ParticipantMergeConsumer;

using System.Threading.Tasks;
using CommonModels.Models.JUSTIN;
using global::Common.Kafka;
using jumwebapi.Data;
using jumwebapi.Data.ef;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using jumwebapi.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class ParticipantMergeConsumerHandler
    (IClock clock, JumWebApiConfiguration configuration, ILogger<ParticipantMergeConsumerHandler> logger, IKafkaProducer<string, ParticipantMergeDetailModel> kafkaProducer, JumDbContext context, IJustinParticipantClient participantClient) : IKafkaHandler<string, ParticipantMergeEvent>
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
            var existingMerge = await context.ParticipantMerges
                .Where(pm => pm.MessageId == key)
                .FirstOrDefaultAsync();

            if (existingMerge == null)
            {
                // Create a new merge entry since no existing record was found
                var newParticipantMerge = new ParticipantMerge
                {
                    MessageId = key,
                    MergeEventTime = DateTime.UtcNow,
                    SourceParticipantId = value.SourceParticipantId,
                    TargetParticipantId = value.TargetParticipantId
                };


                // get information from JUSTIN for the two participants
                var sourceParticipant = await participantClient.GetParticipantByPartId(value.SourceParticipantId, "");
                var targetParticipant = await participantClient.GetParticipantByPartId(value.TargetParticipantId, "");

                // Add the new entry to the ParticipantMerges table
                await context.ParticipantMerges.AddAsync(newParticipantMerge);

                // publish a new message with additional info
                if (sourceParticipant != null && targetParticipant != null && sourceParticipant.participantDetails != null && targetParticipant.participantDetails != null)
                {
                    var msgId = Guid.NewGuid().ToString();
                    newParticipantMerge.PublishedMessageId = msgId;
                    var source = sourceParticipant.participantDetails.FirstOrDefault();
                    var target = targetParticipant.participantDetails.FirstOrDefault();

                    newParticipantMerge.SourceParticipantFirstName = source.firstGivenNm;
                    newParticipantMerge.SourceParticipantLastName = source.surname;
                    var sourceDOBOk = DateOnly.TryParse(source.birthDate, out var sourceDOB);
                    if (sourceDOBOk)
                    {
                        newParticipantMerge.SourceParticipantDOB = sourceDOB;
                    }
                    newParticipantMerge.TargetParticipantFirstName = target.firstGivenNm;
                    newParticipantMerge.TargetParticipantLastName = target.surname;
                    var targetDOBOk = DateOnly.TryParse(target.birthDate, out var targetDOB);
                    if (targetDOBOk)
                    {
                        newParticipantMerge.TargetParticipantDOB = targetDOB;
                    }


                    // we will publish a message to a new topic with more detail, this in-turn will be picked up by other services
                    // such as webapi to see if this participant is onboarded and if they are then we'll publish yet another message to EDT Disclosure
                    // to update case access for the user
                    logger.LogInformation($"Publishing new message for part merge from {sourceParticipant} to {targetParticipant}");
                    var published = kafkaProducer.ProduceAsync(configuration.KafkaCluster.ParticipantMergeResponseTopic, msgId, new ParticipantMergeDetailModel()
                    {
                        CreatedOn = clock.GetCurrentInstant(),
                        MergeId = newParticipantMerge.Id,
                        ParticipantType = ParticipantType.ACCUSED,
                        SourceParticipant = source,
                        TargetParticipant = target
                    });

                }
                else
                {
                    logger.LogError($"Source or target participant not found for merge event {value.SourceParticipantId} {value.TargetParticipantId}");
                    return Task.CompletedTask;

                }




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
