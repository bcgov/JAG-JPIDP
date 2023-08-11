namespace ApprovalFlow.ServiceEvents.IncomingApproval;

using System.Security.Cryptography;
using System.Threading.Tasks;
using ApprovalFlow.Data;
using ApprovalFlow.Data.Approval;
using ApprovalFlow.Exceptions;
using Common.Kafka;
using Common.Models.Approval;
using Common.Models.Notification;

public class IncomingApprovalHandler : IKafkaHandler<string, ApprovalRequestModel>
{
    private readonly ApprovalFlowDataStoreDbContext context;
    private readonly IKafkaProducer<string, Notification> producer;

    public IncomingApprovalHandler(
        ApprovalFlowDataStoreDbContext approvalFlowDataStoreDbContext,
          IKafkaProducer<string, Notification> producer
        )
    {
        this.context = approvalFlowDataStoreDbContext;
        this.producer = producer;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, ApprovalRequestModel incomingRequest)
    {
        Serilog.Log.Information($"Received approval request {key}");

        // see if we've handled this message before
        var existingRequest = this.context.ApprovalRequests.Where(req => req.MessageKey == key).FirstOrDefault();

        if (existingRequest != null)
        {
            Serilog.Log.Warning($"Approval request already processed - message {key} will be ignored");
            return Task.CompletedTask;
        }
        else
        {
            // make sure we have some access requests


            // create a new entry in the approval tables
            using var trx = this.context.Database.BeginTransaction();

            try
            {
                if (incomingRequest.AccessRequests.Count == 0)
                {
                    throw new IncomingApprovalException($"No access requests in message {key} - request will be ignored");
                }
                if (incomingRequest.Reasons.Count == 0)
                {
                    throw new IncomingApprovalException($"No reasons provided for approal request {key} - request will be ignored");
                }

                Serilog.Log.Information($"Adding new approval request {key} {incomingRequest.UserId}");

                var accessRequests = new List<Data.Approval.Request>();

                // add request info
                foreach (var request in incomingRequest.AccessRequests)
                {
                    accessRequests.Add(new Data.Approval.Request
                    {
                        RequestType = request.RequestType,
                        RequestId = request.AccessRequestId,
                        ApprovalType = ApprovalType.AccessRequest
                    });
                }

                var approvalRequest = new ApprovalRequest
                {
                    MessageKey = key,
                    UserId = incomingRequest.UserId,
                    IdentityProvider = incomingRequest.IdentityProvider,
                    Reason = string.Join(", ", incomingRequest.Reasons),
                    Requests = accessRequests
                };

                // add the entry to the context
                this.context.ApprovalRequests.Add(approvalRequest);

                // save it
                var saved = await this.context.SaveChangesAsync();

                if (saved > 0)
                {
                    Serilog.Log.Information($"New approval request created for {key} {approvalRequest.Id}");
                    await trx.CommitAsync();


                    // send a notification if enabled
                }
                else
                {
                    Serilog.Log.Error($"There was a problem saving request {key}");
                    await trx.RollbackAsync();
                    return Task.FromException(new IncomingApprovalException($"Failed to store request {key} in Db"));
                }


            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
            }

        }
        return Task.CompletedTask;

    }
}
