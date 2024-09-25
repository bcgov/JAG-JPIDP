namespace Pidp.Kafka.Consumer.Responses;

using System;
using System.Collections.Generic;
using Common.Models.Approval;
using Common.Models.EDT;
using Common.Models.Notification;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Prometheus;

public class DomainEventResponseHandler : IKafkaHandler<string, GenericProcessStatusResponse>
{
    private readonly PidpDbContext context;
    private readonly IJumClient jumClient;
    private readonly PidpConfiguration configuration;
    private readonly IClock clock;
    private readonly IKafkaProducer<string, Notification> notificationProducer;
    private readonly IKafkaProducer<string, object> resubmitProducer;

    private static readonly Histogram JUSTINAccessCompletionHistogram
        = Metrics
    .CreateHistogram("justin_account_finalization_histogram", "Histogram of account finalizations within JUSTIN.");
    private static readonly Histogram AccessCompletionHistogram
      = Metrics
  .CreateHistogram("account_provision_histogram", "Histogram of account provisions roundtrips.");

    public DomainEventResponseHandler(PidpDbContext context, IJumClient jumClient, IClock clock, PidpConfiguration configuration, IKafkaProducer<string, Notification> notificationProducer, IKafkaProducer<string, object> resubmitProducer)
    {
        this.context = context;
        this.clock = clock;
        this.jumClient = jumClient;
        this.notificationProducer = notificationProducer;
        this.resubmitProducer = resubmitProducer;
        this.configuration = configuration;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, GenericProcessStatusResponse value)
    {
        Serilog.Log.Information($"Process response received {key} for {value.Id} {value.DomainEvent}");

        //check whether this message has been processed before   
        if (await this.context.HasBeenProcessed(key, consumerName))
        {
            return Task.CompletedTask;
        }

        // ideally there'd be a single process response service that can handle all processing responses
        // and these would be in a separate Db for tracking processes.

        switch (value.DomainEvent)
        {
            case "digitalevidencedisclosure-defence-usercreation-complete":
            case "digitalevidencedisclosure-defence-usermodification-complete":
            case "digitalevidencedisclosure-defence-usermodification-error":
            case "digitalevidencedisclosure-defence-usercreation-exception":
            case "digitalevidencedisclosure-defence-usercreation-error":
            case "digitalevidence-defence-personmodification-complete":
            case "digitalevidence-defence-personmodification-error":
            case "digitalevidence-defence-personcreation-complete":
            case "digitalevidence-defence-personcreation-error":
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for Id {value.Id}");
                // todo - this should move to a generic service
                await this.MarkDefenceProcessComplete(value);
                break;
            }

            case "digitalevidencedisclosure-bcsc-usercreation-complete":
            case "digitalevidencedisclosure-bcsc-usercreation-error":
            case "digitalevidencedisclosure-bcsc-usermodification-complete":
            case "digitalevidencedisclosure-bcsc-usermodification-error":
            case "digitalevidencedisclosure-bcsc-exception": // maybe this needs to become a generic exception type?
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for Public User Id {value.Id}");
                // todo - this should move to a generic service
                await this.MarkPublicUserProcessComplete(value);
                break;
            }

            case "digitalevidence-approvalresponse-complete":
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for approval flow response {value.Id}");
                await this.MarkApprovalFlowComplete(value);
                break;
            }

            case "digitalevidence-bcps-edt-userupdate-complete":
            case "digitalevidence-bcps-edt-userupdate-error":
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for JustinUserChange {value.Id}");
                // todo - this should move to a generic service
                await this.UpdateUserChangeStatus(value);
                break;
            }
            case "digitalevidence-bcps-usercreation-accountfinalized":
            case "digitalevidence-bcps-usercreation-accountfinalized-error":

            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for account fully provisioned {value.Id}");
                // todo - this should move to a generic service
                await this.MarkAccountFullyProvisioned(value);
                break;
            }
            case "digitalevidence-court-location-provision-complete":
            case "digitalevidence-court-location-provision-error":
            case "digitalevidence-court-location-decommission-complete":
            case "digitalevidence-court-location-decommission-error":

            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for Court Location Request {value.Id}");
                // todo - this could move to a generic service
                await this.MarkCourtLocationProcessResponse(value);
                break;
            }

            case "digitalevidence-processresponse-topic":
            {
                Serilog.Log.Information($"Handling {value.DomainEvent} for JAM User Provisioning Request {value.Id}");
                // todo - this could move to a generic service
                await this.MarkJAMUserProvisionProcessResponse(value);
                break;
            }
            default:
            {
                Serilog.Log.Warning($"Ignoring unhandled domain event process {value.DomainEvent}");
                break;
            }
        }

        //add to tell message has been processed by consumer
        await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
        await this.context.SaveChangesAsync();

        return Task.CompletedTask;

    }

    private async Task MarkCourtLocationProcessResponse(GenericProcessStatusResponse processResponse)
    {
        var accessRequest = this.context.CourtLocationAccessRequests.Include(req => req.Party).Where(req => req.RequestId == processResponse.Id).FirstOrDefault();
        if (accessRequest == null)
        {
            Serilog.Log.Warning($"Received notification for non-existent court request {processResponse.Id} - ignoring");
            return;
        }

        Serilog.Log.Information($"Court location request {processResponse.Id} flagged as {processResponse.Status}");
        accessRequest.RequestStatus = processResponse.Status;
        if (processResponse.Status == CourtLocationAccessStatus.Deleted)
        {
            accessRequest.DeletedOn = processResponse.EventTime;
        }

        if (processResponse.Status == "Error")
        {
            accessRequest.Details = string.Join(",", processResponse.ErrorList);
        }

        var updated = await this.context.SaveChangesAsync();
        if (updated > 0)
        {
            Serilog.Log.Information($"Process marked as complete for {accessRequest.RequestId}");
        }

    }

    private async Task MarkJAMUserProvisionProcessResponse(GenericProcessStatusResponse processResponse)
    {
        var accessRequest = this.context.AccessRequests.Include(req => req.Party).Where(req => req.Id == processResponse.Id).FirstOrDefault();
        if (accessRequest != null)
        {

            if (processResponse.Status == "Complete")
            {
                accessRequest.Status = "Complete";
            }
            else
            {
                accessRequest.Status = processResponse.Status;
                accessRequest.Details = string.Join(",", processResponse.ErrorList);
            }

            var updated = await this.context.SaveChangesAsync();


        }

     }

    private async Task MarkDisclosureFullyProvisioned(GenericProcessStatusResponse processResponse)
    {
        var accessRequest = this.context.AccessRequests.Include(req => req.Party).Where(req => req.Id == processResponse.Id).FirstOrDefault();
        if (accessRequest != null)
        {

            if (processResponse.Status == "Complete")
            {
                accessRequest.Status = "Complete";
            }
            else
            {
                accessRequest.Status = processResponse.Status;
                accessRequest.Details = string.Join(",", processResponse.ErrorList);
            }

            var updated = await this.context.SaveChangesAsync();
            if (updated > 0)
            {
                // send a notification to the user that the account is complete/errored
                var messageKey = Guid.NewGuid().ToString();
                var duration = accessRequest.Modified - processResponse.EventTime;
                AccessCompletionHistogram.Observe(duration.TotalMilliseconds);
                Serilog.Log.Information($"Access request {processResponse.Id} flagged as {processResponse.Status} - sending notification message {messageKey}");
                var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequest.Party!.FirstName },
                        { "PartyId", "" + accessRequest.Id },
                        { "Duration (s)","" + duration.TotalSeconds }
                    };

                var published = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
                {
                    DomainEvent = processResponse.DomainEvent,
                    To = accessRequest.Party!.Email,
                    EventData = eventData
                });

                Serilog.Log.Information($"Publish response for {messageKey} is {published.Status}");

            }

        }

    }

    private async Task MarkPublicUserProcessComplete(GenericProcessStatusResponse processResponse)
    {
        var accessRequest = this.context.AccessRequests.Include(req => req.Party).Where(req => req.Id == processResponse.Id).FirstOrDefault();

        if (accessRequest == null)
        {
            Serilog.Log.Warning($"Message received for non-existent access request {processResponse.Id} - ignoring");
            return;
        }

        accessRequest.Modified = processResponse.EventTime;
        // todo - fix these to be constant/and consistent types - need grafana updates too
        if (processResponse.ErrorList != null && processResponse.ErrorList.Count > 0)
        {
            accessRequest.Status = "Error";
            accessRequest.Details = string.Join(",", processResponse.ErrorList);

            var duration = accessRequest.Modified - processResponse.EventTime;
            var messageKey = Guid.NewGuid().ToString();

            var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequest.Party!.FirstName },
                        { "BCSC Id", accessRequest.Party.Jpdid },
                        { "PartyId", "" + accessRequest.Party.Id },
                        { "Errors", accessRequest.Details },
                        { "Duration (s)","" + duration.TotalSeconds }

                    };
            var published = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
            {
                DomainEvent = "digitalevidence-bcsc-usercreation-error",
                To = this.configuration.EnvironmentConfig.SupportEmail,
                EventData = eventData
            });
        }
        else
        {
            accessRequest.Status = "Complete";
        }



        var updated = await this.context.SaveChangesAsync();


    }


    private async Task MarkDefenceProcessComplete(GenericProcessStatusResponse processResponse)
    {
        var accessRequest = this.context.AccessRequests.Include(req => req.Party).Where(req => req.Id == processResponse.Id).FirstOrDefault();

        if (accessRequest == null)
        {
            Serilog.Log.Warning($"Message received for non-existent access request {processResponse.Id} - ignoring");
            return;
        }

        accessRequest.Modified = processResponse.EventTime;
        if (processResponse.ErrorList != null && processResponse.ErrorList.Count > 0)
        {
            accessRequest.Status = "Error";
            accessRequest.Details = string.Join(",", processResponse.ErrorList);
            var duration = accessRequest.Modified - processResponse.EventTime;
            var messageKey = Guid.NewGuid().ToString();

            var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequest.Party!.FirstName },
                        { "PartyId", "" + accessRequest.Party.Id },
                        { "Errors", accessRequest.Details },
                        { "Duration (s)","" + duration.TotalSeconds }

                    };
            var published = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
            {
                DomainEvent = "digitalevidence-bclaw-usercreation-error",
                To = accessRequest.Party!.Email,
                EventData = eventData
            });

        }
        else
        {
            accessRequest.Status = "Complete";
        }

        // see if both accounts are fully provisioned
        var requests = this.context.AccessRequests.Include(req => req.Party).Where(req => req.PartyId == accessRequest.PartyId).ToList();

        // check if both are done
        var disclosureUserAdded = false;
        var corePersonAdded = false;
        foreach (var request in requests)
        {
            if (request.AccessTypeCode == Models.Lookups.AccessTypeCode.DigitalEvidenceDefence)
            {
                corePersonAdded = request.Status.Equals("Complete");
            }
            if (request.AccessTypeCode == Models.Lookups.AccessTypeCode.DigitalEvidenceDisclosure)
            {
                disclosureUserAdded = request.Status.Equals("Complete");
            }
        }

        if (accessRequest.Status == "Error")
        {
            var messageKey = Guid.NewGuid().ToString();
            var duration = accessRequest.Modified - processResponse.EventTime;

            var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequest.Party!.FirstName },
                        { "PartyId", "" + accessRequest.Party.Id },
                        {  "Errors", accessRequest.Details },
                        { "Duration (s)","" + duration.TotalSeconds }

                    };
            var published = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
            {
                DomainEvent = "digitalevidence-bclaw-usercreation-error",
                To = accessRequest.Party!.Email,
                EventData = eventData
            });
        }

        if (disclosureUserAdded && corePersonAdded)
        {
            var duration = accessRequest.Modified - processResponse.EventTime;

            Serilog.Log.Information($"Notifying user {accessRequest.PartyId}  of account completion");
            var eventData = new Dictionary<string, string>
                    {
                        { "FirstName", accessRequest.Party!.FirstName },
                        { "PartyId", "" + accessRequest.Party.Id },
                        { "Duration (s)","" + duration.TotalSeconds }

                    };

            var messageKey = Guid.NewGuid().ToString();
            var published = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
            {
                DomainEvent = "digitalevidence-bclaw-usercreation-complete",
                To = accessRequest.Party!.Email,
                EventData = eventData
            });
        }

        var updated = await this.context.SaveChangesAsync();
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
            Serilog.Log.Error($"No access request found with party id {value.PartId} for account update");
        }
        else
        {

            if (string.Equals(accessRequest.Status, "Complete", StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Information($"Access request {accessRequest.Id} already marked as complete - ignoring message");
            }
            else
            {

                // get last modified time
                var duration = accessRequest.Modified - value.EventTime;
                Serilog.Log.Information($"Duration {duration.TotalMinutes} for JUSTIN to fully provision account for request {accessRequest.Id}");


                JUSTINAccessCompletionHistogram.Observe(duration.Minutes);

                accessRequest.Modified = value.EventTime;
                if (value.ErrorList != null && value.ErrorList.Count > 0)
                {
                    accessRequest.Status = "Error";
                }
                else
                {
                    accessRequest.Status = "Complete";
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
                        { "Duration (m)","" + duration.TotalMinutes }
                    };

                    var published = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, messageKey, new Notification
                    {
                        DomainEvent = "digitalevidence-bcps-usercreation-fully-provisioned",
                        To = accessRequest.Party!.Email,
                        EventData = eventData
                    });

                    Serilog.Log.Information($"Publish response for {messageKey} is {published.Status}");

                }
            }
        }
    }

    private async Task<DeliveryResult<string, object>> ResubmitRequest(string topic, object disclosureUserRequest)
    {
        var taskId = Guid.NewGuid().ToString();
        Serilog.Log.Logger.Information("Adding message to topic {0} {1}", topic, taskId);


        // use UUIDs for topic keys
        var delivered = await this.resubmitProducer.ProduceAsync(topic,
            taskId, disclosureUserRequest);

        return delivered;
    }

    private async Task MarkApprovalFlowComplete(GenericProcessStatusResponse response)
    {
        try
        {
            var decisionNotes = "";
            Party party = null;
            var requestData = response.ResponseData["approvalModel"];
            var originalRequest = JsonConvert.DeserializeObject<ApprovalModel>(requestData,
                   new JsonSerializerSettings
                   {
                       MissingMemberHandling = MissingMemberHandling.Ignore,
                       DateParseHandling = DateParseHandling.None

                   });

            foreach (var request in originalRequest.Requests)
            {
                Serilog.Log.Information($"Marking request {request.RequestType} {request.RequestId} as {response.Status}");
                var dbRequest = this.context.AccessRequests.AsSplitQuery().Include(req => req.Party).Where(req => req.Id == request.RequestId).FirstOrDefault();
                if (dbRequest != null)
                {
                    dbRequest.Status = response.Status.ToString();
                    dbRequest.Modified = response.EventTime;


                    if (response.ErrorList != null && response.ErrorList.Count > 0)
                    {
                        Serilog.Log.Warning($"Approval Event {response.Id} came back with errors [{string.Join(",", response.ErrorList)}]");
                    }
                    else
                    {
                        if (response.Status == "Approved")
                        {
                            if (request.RequestType.Equals("DigitalEvidenceDefence", StringComparison.Ordinal))
                            {
                                var deferredCorePersonEvent = this.context.DeferredEvents.Where(req => req.RequestId == request.RequestId && req.EventType == "defence-person-creation").FirstOrDefault();
                                if (deferredCorePersonEvent != null)
                                {

                                    var payload = JsonConvert.DeserializeObject<EdtPersonProvisioningModel>(deferredCorePersonEvent.EventPayload);

                                    var delivered = await this.ResubmitRequest(this.configuration.KafkaCluster.PersonCreationTopic, payload);
                                    if (delivered.Status == PersistenceStatus.Persisted)
                                    {
                                        dbRequest.Status = "Pending";
                                        Serilog.Log.Information($"Message was resubmitted - removing from deferred events");
                                        this.context.DeferredEvents.Remove(deferredCorePersonEvent);
                                    }
                                    else
                                    {
                                        Serilog.Log.Error($"Failed to resubmit event for request {request.RequestId}");
                                    }

                                }
                            }

                            if (request.RequestType.Equals("DigitalEvidenceDisclosure", StringComparison.Ordinal))
                            {
                                var deferredDisclosureUserCreation = this.context.DeferredEvents.Where(req => req.RequestId == request.RequestId && req.EventType == "disclosure-user-creation").FirstOrDefault();
                                if (deferredDisclosureUserCreation != null)
                                {
                                    var payload = JsonConvert.DeserializeObject<EdtDisclosureUserProvisioning>(deferredDisclosureUserCreation.EventPayload);

                                    var delivered = await this.ResubmitRequest(this.configuration.KafkaCluster.DisclosureDefenceUserCreationTopic, payload);
                                    if (delivered.Status == PersistenceStatus.Persisted)
                                    {
                                        dbRequest.Status = "Pending";
                                        Serilog.Log.Information($"Message was resubmitted - removing from deferred events");
                                        this.context.DeferredEvents.Remove(deferredDisclosureUserCreation);

                                    }
                                    else
                                    {
                                        Serilog.Log.Error($"Failed to resubmit event for request {request.RequestId}");
                                    }
                                }
                            }
                        }
                    }

                    var lastHistory = request.History.LastOrDefault();
                    if (lastHistory != null)
                    {
                        if (lastHistory.DecisionNote != decisionNotes)
                        {
                            decisionNotes += lastHistory.DecisionNote;
                        }
                    }

                }

                party = dbRequest.Party;
            }
            var changeCount = await this.context.SaveChangesAsync();
            Serilog.Log.Information($"{changeCount} rows updated");

            if (!string.IsNullOrEmpty(party.Email))
            {
                var msgKey = Guid.NewGuid().ToString();
                Serilog.Log.Information($"Notifying user {party.Email} of decision for {originalRequest.Id}");
                var delivered = await this.notificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, msgKey, new Notification
                {
                    DomainEvent = originalRequest.Approved != null ? "digitalevidence-approvalrequest-approved" : "digitalevidence-approvalrequest-denied",
                    To = party.Email,
                    EventData = new Dictionary<string, string> {
                        { "firstName",party.FirstName }
                        ,{  "decisionNotes", decisionNotes                        }

                    }
                });

                if (delivered.Status == PersistenceStatus.Persisted)
                {
                    Serilog.Log.Information($"Message {msgKey} send to notification topic part {delivered.Partition.Value}");
                }
                else
                {
                    Serilog.Log.Error($"Failed to send message with key {msgKey} {delivered.Status}");
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Failed to process {response.Id}: {ex.Message}");
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
