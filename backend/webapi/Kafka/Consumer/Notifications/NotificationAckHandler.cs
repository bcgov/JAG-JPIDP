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

        Log.Logger.Information("Message received on {0} with key {1}", consumerName, key);
        //check whether this message has been processed before   
        if (await this.context.HasBeenProcessed(key, consumerName))
        {
            return Task.CompletedTask;
        }

        // access request notification
        if (value.Subject.Equals(NotificationSubject.None) || value.Subject.Equals(NotificationSubject.AccessRequest))
        {

            var accessRequest = await this.context.AccessRequests
                .Where(request => request.Id == value.AccessRequestId).SingleOrDefaultAsync();
            if (accessRequest != null)
            {
                Log.Information($"Marking access request {value.AccessRequestId} as {value.Status}");

                using var trx = this.context.Database.BeginTransaction();

                try
                {
                    accessRequest.Status = value.Status;
                    await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
                    await this.context.SaveChangesAsync();
                    trx.Commit();

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
                using var trx = this.context.Database.BeginTransaction();

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
                        accessRequest.RequestStatus = value.Status;
                        accessRequest.Details = value.Details;
                    }

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
        }

        return Task.FromException(new InvalidOperationException()); //create specific exception handler later
    }
}
