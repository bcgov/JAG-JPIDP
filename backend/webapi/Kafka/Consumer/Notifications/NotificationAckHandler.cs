namespace Pidp.Kafka.Consumer.Notifications;

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Serilog;

/// <summary>
/// Handle notifications coming back on topics
/// </summary>
/// <param name="context"></param>
public class NotificationAckHandler(PidpDbContext context, IClock clock) : IKafkaHandler<string, NotificationAckModel>
{
    public async Task<Task> HandleAsync(string consumerName, string key, NotificationAckModel value)
    {

        using var trx = context.Database.BeginTransaction();

        Log.Logger.Information($"{value.PartId} {value.EventType} Message received on {consumerName} with key {key}");
        //check whether this message has been processed before
        if (await context.HasBeenProcessed(key, consumerName))
        {
            await trx.RollbackAsync();
            return Task.CompletedTask;
        }

        // access request notification
        if (value.Subject.Equals(NotificationSubject.None) || value.Subject.Equals(NotificationSubject.AccessRequest))
        {

            var accessRequest = await context.AccessRequests
                .Where(request => request.Id == value.AccessRequestId).SingleOrDefaultAsync();
            if (accessRequest != null)
            {
                Log.Information($"Marking access request {value.AccessRequestId} {value.PartId} as {value.Status}");

                try
                {
                    accessRequest.Status = value.Status;

                    await context.IdempotentConsumer(messageId: key, consumer: consumerName);
                    await context.SaveChangesAsync();
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
                Log.Error($"{value.PartId} {value.EventType} - access request {value.AccessRequestId} is unknown");
                return Task.CompletedTask;
            }
        }

        if (value.Subject.Equals(NotificationSubject.CaseAccessRequest))
        {
            var accessRequest = await context.SubmittingAgencyRequests
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
                        // soft delete
                        accessRequest.DeletedOn = clock.GetCurrentInstant();
                        accessRequest.RequestStatus = AgencyRequestStatus.Deleted;
                        // context.Entry(accessRequest).State = EntityState.Deleted;
                    }
                    else
                    {
                        Log.Information($"Flagging {value.Status} for {accessRequest.RequestId}");
                        accessRequest.RequestStatus = value.Status;
                        accessRequest.Details = string.IsNullOrEmpty(value.Details) ? value.Status : value.Details;
                    }


                    var affectedRows = await context.SaveChangesAsync();
                    if (affectedRows > 0)
                    {
                        await context.IdempotentConsumer(messageId: key, consumer: consumerName);
                        await trx.CommitAsync();
                    }
                    return Task.CompletedTask;
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
