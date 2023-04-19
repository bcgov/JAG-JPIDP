namespace Pidp.Kafka.Consumer.Responses;

using System;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Kafka.Interfaces;
using Pidp.Models;

public class DomainEventResponseHandler : IKafkaHandler<string, GenericProcessStatusResponse>
{
    private readonly PidpDbContext context;
    private readonly JumClient jumClient;

    public DomainEventResponseHandler(PidpDbContext context, JumClient jumClient)
    {
        this.context = context;
        this.jumClient = jumClient;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, GenericProcessStatusResponse value)
    {
        Serilog.Log.Information($"Process response received {key} for {value.Id} {value.DomainEvent}");

        switch (value.DomainEvent)
        {
            case "digitalevidence-bcps-edt-userupdate-complete":
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for JustinUserChange {value.Id}");
                // todo - this could move to a generic service
                await this.UpdateUserChangeStatus(value);
                break;
            }
            default:
            {
                Serilog.Log.Warning($"Ignoring unhandled domain event process {value.DomainEvent}");
                break;
            }
        }

        return Task.CompletedTask;

    }

    private async Task UpdateUserChangeStatus(GenericProcessStatusResponse value)
    {
        var userChangeEntry = this.context.UserAccountChanges.Where(change => change.Id == value.Id).FirstOrDefault();

        if ( userChangeEntry == null)
        {
            Serilog.Log.Error($"No UserAccountChange entry found with id {value.Id}");
        }
        else
        {
            userChangeEntry.Status = value.Status;
            userChangeEntry.Modified = value.EventTime;
            if (value.ErrorList != null && value.ErrorList.Count > 0)
            {
                Serilog.Log.Warning($"UserChange Event {value.Id} came back with errors [{string.Join(",", value.ErrorList)}]");
                // todo - store errors in a different table ?

            }

            // if the status is completed then update JUSTIN that this item is processed
            if ( value.Status.Equals("Complete", StringComparison.Ordinal))
            {
                Serilog.Log.Information($"Flagging change item {value.Id} with changeId {userChangeEntry.EventMessageId}");
                var response = await this.jumClient.FlagUserUpdateAsComplete(userChangeEntry.EventMessageId, true);

            }


            await this.context.SaveChangesAsync();
        }
    }
}
