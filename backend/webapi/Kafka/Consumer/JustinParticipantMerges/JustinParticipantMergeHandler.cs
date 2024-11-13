namespace Pidp.Kafka.Consumer.JustinParticipantMerges;

using CommonModels.Models.JUSTIN;
using DomainResults.Common;
using Pidp.Data;
using Pidp.Features;
using Pidp.Kafka.Interfaces;
using Serilog;
using static Pidp.Features.AccessRequests.DigitalEvidenceUpdate;

public class JustinParticipantMergeHandler(PidpDbContext context, ICommandHandler<Features.AccessRequests.DigitalEvidenceUpdate.Command, IDomainResult> commandHandler) : IKafkaHandler<string, ParticipantMergeDetailModel>
{
    private readonly PidpDbContext context = context;
    private readonly ICommandHandler<Command, IDomainResult> commandHandler = commandHandler;

    public async Task<Task> HandleAsync(string consumerName, string key, ParticipantMergeDetailModel value)
    {

        Log.Logger.Information("Message received on {0} with key {1}", consumerName, key);


        return Task.CompletedTask;

    }
}
