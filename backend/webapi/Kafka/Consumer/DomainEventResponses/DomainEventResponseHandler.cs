namespace Pidp.Kafka.Consumer.Responses;

using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Prometheus;

public class DomainEventResponseHandler : IKafkaHandler<string, GenericProcessStatusResponse>
{
    private readonly PidpDbContext context;
    private readonly JumClient jumClient;
    private readonly PidpConfiguration configuration;
    private readonly IKafkaProducer<string, Notification> producer;
    private static readonly Histogram AccessCompletionHistogram = Metrics
    .CreateHistogram("account_finalization_histogram", "Histogram of account finalizations.");

    public DomainEventResponseHandler(PidpDbContext context, JumClient jumClient, PidpConfiguration configuration, IKafkaProducer<string, Notification> producer
)
    {
        this.context = context;
        this.jumClient = jumClient;
        this.producer = producer;
        this.configuration = configuration;
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
            case "digitalevidence-bcps-usercreation-accountfinalized":
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for account fully provisioned {value.Id}");
                // todo - this could move to a generic service
                await this.MarkAccountFullyProvisioned(value);
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

    private async Task MarkAccountFullyProvisioned(GenericProcessStatusResponse value)
    {


        AccessRequest accessRequest = null;

        if (value.Id > 0)
        {
            accessRequest = this.context.AccessRequests.Where(req => req.Id == value.Id).FirstOrDefault();
        }
        else
        {
            var justiceOrgDetail = this.context.JusticeSectorDetails.Include(jsd => jsd.OrgainizationDetail).Where(justiceOrgDetail => justiceOrgDetail.ParticipantId == value.PartId).FirstOrDefault();
            if (justiceOrgDetail != null)
            {
                var accessRequests = this.context.AccessRequests.Include(req => req.Party).Where(accessRequest => accessRequest.PartyId == justiceOrgDetail.OrgainizationDetail.PartyId && accessRequest.AccessTypeCode == Models.Lookups.AccessTypeCode.DigitalEvidence).ToList();
                if (accessRequests.Any())
                {
                    if (accessRequests.Count == 1)
                    {
                        accessRequest = accessRequests.First();
                    }
                    else
                    {
                        Serilog.Log.Warning($"Multiple requests found for participant {value.PartId} for DEMS - using latest entry");
                        accessRequest = accessRequests.OrderByDescending(e => e.Created).FirstOrDefault();
                    }
                }
            }
        }


        if (accessRequest == null)
        {
            Serilog.Log.Error($"No access request found with id {value.Id} for account update");
        }
        else
        {
            // get last modified time
            var duration = accessRequest.Modified - value.EventTime;
            Serilog.Log.Error($"Duration {duration.TotalMinutes} for JUSTIN to fully provision account for request {accessRequest.Id}");


            AccessCompletionHistogram.Observe(duration.Minutes);

            accessRequest.Modified = value.EventTime;
            if (value.ErrorList != null && value.ErrorList.Count > 0)
            {
                accessRequest.Status = "Errored";
            }
            else
            {
                accessRequest.Status = "Completed";
            }

            var updated = await this.context.SaveChangesAsync();
            if (updated > 0)
            {
                // send a notification to the user that the account is complete
                var messageKey = Guid.NewGuid().ToString();
                Serilog.Log.Information($"Access request {value.Id} flagged as fully completed - sending notification message {messageKey}");
                var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequest.Party!.FirstName },
                        { "PartyId", "" + accessRequest.Id },
                        { "Duration","" + duration.TotalMinutes }
                    };

                var published = await this.producer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
                {
                    DomainEvent = "digitalevidence-bcps-usercreation-fully-provisioned",
                    To = accessRequest.Party!.Jpdid,
                    EventData = eventData
                });

                Serilog.Log.Information($"Publish response for {messageKey} is {published.Status}");

            }
        }
    }

    private async Task UpdateUserChangeStatus(GenericProcessStatusResponse value)
    {
        var userChangeEntry = this.context.UserAccountChanges.Where(change => change.Id == value.Id).FirstOrDefault();

        if (userChangeEntry == null)
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
            if (value.Status.Equals("Complete", StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Information($"Flagging change item {value.Id} with changeId {userChangeEntry.EventMessageId}");
                var response = await this.jumClient.FlagUserUpdateAsComplete(userChangeEntry.EventMessageId, true);

            }


            await this.context.SaveChangesAsync();
        }
    }
}
