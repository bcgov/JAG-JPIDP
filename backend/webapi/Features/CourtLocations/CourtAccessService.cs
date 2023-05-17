namespace Pidp.Features.CourtLocations;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using NodaTime;
using Pidp.Data;
using Pidp.Features.CourtLocations.Commands;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;

public class CourtAccessService : ICourtAccessService
{
    private readonly IClock clock;
    private readonly PidpConfiguration config;
    private readonly PidpDbContext context;
    private readonly IKafkaProducer<string, CourtLocationDomainEvent> kafkaProducer;

    public CourtAccessService(IClock clock, PidpConfiguration config, PidpDbContext context, IKafkaProducer<string, CourtLocationDomainEvent> kafkaProducer)
    {
        this.clock = clock;
        this.config = config;
        this.context = context;
        this.kafkaProducer = kafkaProducer;
    }

    public async Task<bool> CreateAddCourtAccessDomainEvent(CourtLocationAccessRequest request) => await this.CreateCourtAccessDomainEvent(request, CourtLocationEventType.Provisioning);

    public async Task<bool> CreateRemoveCourtAccessDomainEvent(CourtLocationAccessRequest request) => await this.CreateCourtAccessDomainEvent(request, CourtLocationEventType.Decommission);

    public async Task<bool> CreateCourtAccessDomainEvent(CourtLocationAccessRequest request, string eventType)
    {

        var isSuccess = true;
        var accessRequestRecord = this.context.CourtLocationAccessRequests.Include(req => req.Party).Where(req => req.RequestId == request.RequestId).FirstOrDefault();

        // update database with message Id
        if (accessRequestRecord != null)
        {
            try
            {

                var courtLocationDomainEvent = new CourtLocationDomainEvent
                {
                    CourtLocation = request.CourtLocation!.Code,
                    PartyId = request.PartyId,
                    RequestedOn = request.RequestedOn,
                    UserId = accessRequestRecord.Party!.UserId,
                    EventType = eventType,
                    RequestId = request.RequestId,
                    ValidFrom = request.ValidFrom,
                    ValidUntil = request.ValidUntil
                };

                var sample = JsonConvert.SerializeObject(courtLocationDomainEvent);

                var messageKey = Guid.NewGuid();
                accessRequestRecord.MessageId = messageKey;

                var response = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.CourtLocationAccessRequestTopic, messageKey.ToString(), courtLocationDomainEvent);
                Serilog.Log.Information($"Sent {request.RequestId} to topic {this.config.KafkaCluster.CourtLocationAccessRequestTopic} Part: {response.Partition.Value} Key: {messageKey}");

                await this.context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Failed to send {request.RequestId} to topic {this.config.KafkaCluster.CourtLocationAccessRequestTopic} [{string.Join(",", ex.Message)}]");
                isSuccess = false;
            }
        }
        else
        {
            Serilog.Log.Error($"No request found with id {request.RequestId}");
            isSuccess = false;

        }

        return isSuccess;
    }


    public async Task<CourtLocationAccessRequest> CreateCourtLocationRequest(CourtAccessRequest.Command command, CourtLocation location)
    {

        var status = command.ValidFrom.DayOfYear == DateTime.Now.DayOfYear ? CourtLocationAccessStatus.Submitted : CourtLocationAccessStatus.SubmittedFuture;

        var courtLocationRequest = new CourtLocationAccessRequest
        {
            CourtLocation = location,
            RequestStatus = status,
            ValidFrom = command.ValidFrom,
            ValidUntil = command.ValidUntil,
            PartyId = command.PartyId,
            RequestedOn = this.clock.GetCurrentInstant()
        };

        this.context.CourtLocationAccessRequests.Add(courtLocationRequest);

        await this.context.SaveChangesAsync();

        return courtLocationRequest;
    }

    /// <summary>
    /// Get any requests due today - either add or remove requests
    /// </summary>
    /// <returns></returns>
    public async Task<List<CourtLocationAccessRequest>> GetRequestsDueToday()
    {

        var requests = this.context.CourtLocationAccessRequests.Include(req => req.Party).Include(req => req.CourtLocation).Where((req) => (req.MessageId == null && req.ValidFrom <= DateTime.Now && req.DeletedOn == null) || (req.MessageId != null && req.ValidUntil <= DateTime.Now && req.DeletedOn == null)).ToList();
        Serilog.Log.Information($"Returning {requests.Count} requests");

        return requests;

    }

    public async Task<bool> DeleteAccessRequest(CourtLocationAccessRequest request)
    {

        try
        {
            Serilog.Log.Information($"Deleting access to {request.RequestId}");

            var accessRequest = this.context.CourtLocationAccessRequests.Include(req => req.Party).Where(req => req.RequestId == request.RequestId).FirstOrDefault();

            if (accessRequest != null)
            {
                accessRequest.RequestStatus = CourtLocationAccessStatus.Deleted;
                accessRequest.DeletedOn = this.clock.GetCurrentInstant();
                await this.context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Failed to set request {request.RequestId} to deleted");
        }

        return true;
    }

    public async Task<bool> SetAccessRequestStatus(CourtLocationAccessRequest request, string status)
    {
        Serilog.Log.Information($"Setting court access request {request.RequestId} to {status}");
        request.RequestStatus = status;
        request.Modified = this.clock.GetCurrentInstant();
        var responseCode = await this.context.SaveChangesAsync();
        return responseCode > 0;

    }
}
