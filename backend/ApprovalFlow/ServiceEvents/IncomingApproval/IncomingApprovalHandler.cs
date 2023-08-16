namespace ApprovalFlow.ServiceEvents.IncomingApproval;

using System.Reflection.Metadata;
using System.Threading.Tasks;
using ApprovalFlow.Data;
using ApprovalFlow.Data.Approval;
using ApprovalFlow.Exceptions;
using ApprovalFlow.Features.WebSockets;
using Common.Kafka;
using Common.Models.Approval;
using Common.Models.Notification;
using Confluent.Kafka;

public class IncomingApprovalHandler : IKafkaHandler<string, ApprovalRequestModel>
{
    private readonly ApprovalFlowDataStoreDbContext context;
    private readonly IKafkaProducer<string, Notification> producer;
    private readonly ApprovalFlowConfiguration configuration;
    private readonly WebSocketService websocketService = WebSocketService.GetInstance();

    public IncomingApprovalHandler(
        ApprovalFlowDataStoreDbContext approvalFlowDataStoreDbContext,
        ApprovalFlowConfiguration config,
          IKafkaProducer<string, Notification> producer
        )
    {
        this.context = approvalFlowDataStoreDbContext;
        this.configuration = config;
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



            using var trx = this.context.Database.BeginTransaction();

            try
            {
                // make sure we have some access requests

                if (incomingRequest.AccessRequests.Count == 0)
                {
                    throw new IncomingApprovalException($"No access requests in message {key} - request will be ignored");
                }
                if (incomingRequest.Reasons.Count == 0)
                {
                    throw new IncomingApprovalException($"No reasons provided for approal request {key} - request will be ignored");
                }

                Serilog.Log.Information($"Adding new approval request {key} {incomingRequest.UserId}");

                var accessRequests = new List<Request>();

                // add request info
                foreach (var request in incomingRequest.AccessRequests)
                {
                    accessRequests.Add(new Request
                    {
                        RequestType = request.RequestType,
                        RequestId = request.AccessRequestId,
                        ApprovalType = ApprovalType.AccessRequest
                    });
                }

                var approvalRequest = new ApprovalRequest
                {
                    MessageKey = key,
                    RequiredAccess = incomingRequest.RequiredAccess,
                    UserId = incomingRequest.UserId,
                    IdentityProvider = incomingRequest.IdentityProvider,
                    Reason = string.Join(", ", incomingRequest.Reasons),
                    NoOfApprovalsRequired = incomingRequest.NoOfApprovalsRequired > 0 ? incomingRequest.NoOfApprovalsRequired : 1,
                    Requests = accessRequests
                };

                // add the entry to the context
                this.context.ApprovalRequests.Add(approvalRequest);

                // create a new entry in the approval tables

                var saved = await this.context.SaveChangesAsync();

                if (saved > 0)
                {
                    Serilog.Log.Information($"New approval request created for {key} {approvalRequest.Id}");
                    await trx.CommitAsync();

                    // broadcast to any listening clients
                    this.websocketService.Broadcast($"New approval {approvalRequest.Id}");

                    var data = new Dictionary<string, string> {
                            { "reasons", string.Join(",",incomingRequest.Reasons )},
                            { "user", incomingRequest.UserId },
                            { "firstName", incomingRequest.FirstName},
                            { "idp", incomingRequest.IdentityProvider  }
                        };

                    // send a notification if enabled to admin email address(es)
                    if (!string.IsNullOrEmpty(incomingRequest.EMailAddress))
                    {
                        var notifyKey = Guid.NewGuid().ToString();
                        var notified = await this.producer.ProduceAsync( this.configuration.KafkaCluster.NotificationTopic, notifyKey, new Notification
                        {
                            To = this.configuration.ApprovalConfig.NotifyEmail,
                            DomainEvent = "digitalevidence-approvalrequest-created",
                            Subject = this.configuration.ApprovalConfig.Subject,
                            EventData = data
                        });

                        if (notified.Status == PersistenceStatus.Persisted)
                        {
                            Serilog.Log.Information($"Entry {notifyKey} was delivered {notified.Partition.Value} for {this.configuration.ApprovalConfig.NotifyEmail}");
                        }
                        else
                        {
                            Serilog.Log.Error($"There was an error delivering to {this.configuration.KafkaCluster.NotificationTopic} for message {notifyKey}");
                        }
                    }
                    else
                    {
                        Serilog.Log.Information($"No email address was provided for {incomingRequest.UserId} [{approvalRequest.Id} - unable to send notification email");
                    }


                    if (!string.IsNullOrEmpty(incomingRequest.EMailAddress))
                    {
                        var messageKey = Guid.NewGuid().ToString();

                        var domainEvent = approvalRequest.IdentityProvider == "verified" ? "digitalevidence-bclaw-approvalrequest-created" : "digitalevidence-bcsc-approvalrequest-created";

                        var delivered = await this.producer.ProduceAsync( this.configuration.KafkaCluster.NotificationTopic, messageKey, new Notification
                        {
                            To = this.configuration.ApprovalConfig.NotifyEmail,
                            DomainEvent = domainEvent,
                            Subject = this.configuration.ApprovalConfig.Subject,
                            EventData = data
                        });

                        if (delivered.Status == PersistenceStatus.Persisted)
                        {
                            Serilog.Log.Information($"Message {messageKey} was delivered {delivered.Partition.Value}");
                        }
                        else
                        {
                            Serilog.Log.Error($"There was an error delivering to {this.configuration.KafkaCluster.NotificationTopic} for message {messageKey}");
                        }

                    }




                }
                else
                {
                    Serilog.Log.Error($"There was a problem saving request {key}");
                    return Task.FromException(new IncomingApprovalException($"Failed to store request {key} in Db"));
                }


            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Error during approval processing {ex.Message}");
                await trx.RollbackAsync();
            }

        }
        return Task.CompletedTask;

    }
}
