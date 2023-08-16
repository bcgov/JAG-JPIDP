namespace ApprovalFlow.Features.Approvals;

using ApprovalFlow.Data;
using ApprovalFlow.Exceptions;
using Common.Models.Approval;
using FluentValidation;
using MediatR;
using NodaTime;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Common.Kafka;
using DIAM.Common.Models;
using Newtonsoft.Json;
using Serilog;

public class ApprovalResponseCommand : IRequestHandler<ApproveDenyInput, ApprovalModel>
{
    private readonly ApprovalFlowDataStoreDbContext dbContext;
    private readonly IClock clock;
    private readonly IMapper mapper;
    private readonly ApprovalFlowConfiguration configuration;
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> producer;

    public ApprovalResponseCommand(ApprovalFlowDataStoreDbContext dbContext,
        IClock clock,
        IMapper mapper,
        IKafkaProducer<string, GenericProcessStatusResponse> producer, ApprovalFlowConfiguration configuration)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.mapper = mapper;
        this.producer = producer;
        this.configuration = configuration;
    }



    public class CommandValidator : AbstractValidator<ApproveDenyInput>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.ApprovalRequestId).GreaterThan(0);
        }
    }

    public async Task<ApprovalModel> Handle(ApproveDenyInput input, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Handling incoming approval request {input.ApprovalRequestId} Approver {input.ApproverUserId} - Approved {input.Approved}");

        var trx = this.dbContext.Database.BeginTransaction();
        // check request valid for approval
        var approvalEntity = this.dbContext.ApprovalRequests.AsSplitQuery().Include(req => req.Requests).ThenInclude(req => req.History).Where(req => req.Id == input.ApprovalRequestId).FirstOrDefault();

        if (approvalEntity == null)
        {
            throw new ApprovalResponseException($"No approval request found for ID {input.ApprovalRequestId} - user {input.ApproverUserId}");
        }

        if (input.Approved)
        {
            approvalEntity.Approved = clock.GetCurrentInstant();
        }



        foreach (var request in approvalEntity.Requests)
        {
            Serilog.Log.Information($"Updating request {approvalEntity.Id}.{request.Id} to approved={input.Approved} by {input.ApproverUserId}");

            request.History.Add(new Data.Approval.ApprovalHistory
            {
                AccessRequest = request,
                Approver = input.ApproverUserId,
                Created = clock.GetCurrentInstant(),
                Status = input.Approved ? ApprovalStatus.APPROVED : ApprovalStatus.DENIED,
                DecisionNote = input.DecisionNotes
            });
        }

        approvalEntity.Completed = clock.GetCurrentInstant();

        var addedRows = await this.dbContext.SaveChangesAsync(cancellationToken);
        Serilog.Log.Information($"{addedRows} added for request {input.ApprovalRequestId}");


        await trx.CommitAsync(cancellationToken);

        // publish message for completion of approval
        var allRequestsComplete = true;
        foreach ( var request in  approvalEntity.Requests )
        {
            if ( request.History.Count != approvalEntity.NoOfApprovalsRequired )
            {
                allRequestsComplete = false;
            }
        }

        var responseData = this.mapper.Map<ApprovalModel>(approvalEntity);


        if ( allRequestsComplete )
        {
            var msgId = Guid.NewGuid().ToString();
            Log.Information($"Publishing approval response message {msgId} for {approvalEntity.Id}");



            var responseDataJSON = JsonConvert.SerializeObject(responseData, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });


            var eventData = new Dictionary<string, string>
            {
                { "approved", "" + approvalEntity.Approved },
                { "approvalModel", responseDataJSON }
            };

            var delivered = await this.producer.ProduceAsync(this.configuration.KafkaCluster.ApprovalResponseTopic, msgId, new GenericProcessStatusResponse
            {
                DomainEvent = "digitalevidence-approvalresponse-complete",
                Status = approvalEntity.Approved != null ? "Approved" : "Denied",
                Id = approvalEntity.Id,
                PartId = approvalEntity.UserId,
                ResponseData = eventData
            });

            if ( delivered.Status == Confluent.Kafka.PersistenceStatus.Persisted )
            {
                Log.Information($"Message {msgId} sent to partition {delivered.Partition.Value}");
            }
            else
            {
                Log.Error($"Message {msgId} failed to send {delivered.Status}");
            }


        }

        return responseData;

    }
}
