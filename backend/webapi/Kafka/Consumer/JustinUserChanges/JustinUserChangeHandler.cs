namespace Pidp.Kafka.Consumer.JustinUserChanges;

using DomainResults.Common;
using Pidp.Data;
using Pidp.Features;
using Pidp.Features.AccessRequests;
using Pidp.Kafka.Interfaces;
using Serilog;
using static Pidp.Features.AccessRequests.DigitalEvidenceUpdate;

public class JustinUserChangeHandler : IKafkaHandler<string, JustinUserChangeEvent>
{
    private readonly PidpDbContext context;
    private readonly ICommandHandler<Command, IDomainResult> commandHandler;


    public JustinUserChangeHandler(PidpDbContext context, ICommandHandler<Command, IDomainResult> commandHandler)
    {
        this.context = context;
        this.commandHandler = commandHandler;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, JustinUserChangeEvent value)
    {

        Log.Logger.Information("Message received on {0} with key {1}", consumerName, key);

        // pass this to digitalevidence to handle
        var command = new Command
        {
            UserChangeEvent = value
        };

        var response = this.commandHandler.HandleAsync(command);
        return Task.CompletedTask;

    }
}
