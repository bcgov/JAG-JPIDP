namespace ApprovalFlow.Features.Approvals;

using ApprovalFlow.Data;
using ApprovalFlow.Exceptions;
using Common.Models.Approval;
using FluentValidation;
using MediatR;
using NodaTime;
using Microsoft.EntityFrameworkCore;


public class ApprovalResponseCommand : IRequestHandler<ApproveDenyInput, ApprovalModel>
{
    private readonly ApprovalFlowDataStoreDbContext dbContext;
    private readonly IClock clock;

    public ApprovalResponseCommand(ApprovalFlowDataStoreDbContext dbContext, IClock clock)
    {
        this.dbContext = dbContext;
        this.clock = clock;
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
        var approvalEntity = this.dbContext.ApprovalRequests.Include(req => req.Requests).ThenInclude(req => req.History).Where(req => req.Id == input.ApprovalRequestId).FirstOrDefault();

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

        var addedRows = await this.dbContext.SaveChangesAsync(cancellationToken);
        Serilog.Log.Information($"{addedRows} added for request {input.ApprovalRequestId}");


        await trx.CommitAsync(cancellationToken);

        // determine what to do for notifications

        return new ApprovalModel();
    }
}
