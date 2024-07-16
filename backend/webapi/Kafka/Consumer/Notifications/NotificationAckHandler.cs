namespace Pidp.Kafka.Consumer.Notifications;

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Serilog;

public class NotificationAckHandler : IKafkaHandler<string, NotificationAckModel>
{
    private readonly PidpDbContext context;
    public NotificationAckHandler(PidpDbContext context) => this.context = context;

    public async Task<Task> HandleAsync(string consumerName, string key, NotificationAckModel value)
    {

        using var trx = this.context.Database.BeginTransaction();

        Log.Logger.Information($"Message received on {consumerName} with key {key}");
        //check whether this message has been processed before   
        if (await this.context.HasBeenProcessed(key, consumerName))
        {
            await trx.RollbackAsync();
            return Task.CompletedTask;
        }

        // access request notification
        if (value.Subject.Equals(NotificationSubject.None) || value.Subject.Equals(NotificationSubject.AccessRequest))
        {

            var accessRequest = await this.context.AccessRequests
                .Where(request => request.Id == value.AccessRequestId).SingleOrDefaultAsync();
            if (accessRequest != null)
            {
                Log.Information($"Marking access request {value.AccessRequestId} {value.PartId} as {value.Status}");

                try
                {
                    accessRequest.Status = value.Status;
                    await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
                    await this.context.SaveChangesAsync();
                    await trx.CommitAsync();

                    return Task.CompletedTask;
                }
                catch (Exception)
                {
                    await trx.RollbackAsync();
                    return Task.FromException(new InvalidOperationException());
                }
            }
            else
            {
                Log.Error($"Access request {value.AccessRequestId} is unknown");
                return Task.CompletedTask;
            }
        }

        if (value.Subject.Equals(NotificationSubject.CaseAccessRequest))
        {
            var accessRequest = await this.context.SubmittingAgencyRequests
              .Where(request => request.RequestId == value.AccessRequestId).SingleOrDefaultAsync();
            if (accessRequest != null)
            {

                try
                {

                    if (string.IsNullOrEmpty(value.EventType))
                    {
                        await trx.RollbackAsync();
                        Log.Error("No event type in notification message {0}", value.ToString());
                        return Task.FromException(new InvalidOperationException());
                    }

                    if (value.EventType.Equals(CaseEventType.Decommission, StringComparison.Ordinal))
                    {
                        Log.Information("Removing case access request {0}", accessRequest.RequestId);
                        this.context.Entry(accessRequest).State = EntityState.Deleted;
                    }
                    else
                    {
                        Log.Information($"Flagging {value.Status} for {accessRequest.RequestId}");
                        accessRequest.RequestStatus = value.Status;
                        accessRequest.Details = string.IsNullOrEmpty(value.Details) ? value.Status : value.Details;
                    }

                    var affectedRows = await this.context.SaveChangesAsync();
                    if (affectedRows > 0)
                    {
                        await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
                        await trx.CommitAsync();

                        return Task.CompletedTask;
                    }
                    else
                    {
                        await trx.RollbackAsync();
                        return Task.FromException(new InvalidOperationException($"Failed to update case record {value.AccessRequestId} - {affectedRows} rows updated"));
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"An error occurred processing notification ack {value.PartId} {key} {value.AccessRequestId} {e.Message}");

                    await trx.RollbackAsync();
                    return Task.FromException(new InvalidOperationException());
                }
            }
            else
            {
                Log.Warning($"Case Access message received for unknown request id {value.AccessRequestId} - ignoring");
                return Task.CompletedTask;
            }
        }

        return Task.FromException(new InvalidOperationException()); //create specific exception handler later
    }
}
