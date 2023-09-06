namespace ApprovalFlow.Features.Approvals;

using System.Threading;
using System.Threading.Tasks;
using ApprovalFlow.Data;
using ApprovalFlow.Data.Approval;
using AutoMapper;

using Common.Models.Approval;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record ApprovalsQuery(bool PendingOnly) : IRequest<IList<ApprovalModel>>;

public class PendingApprovalQueryHandler : IRequestHandler<ApprovalsQuery, IList<ApprovalModel>>
{
    private readonly ApprovalFlowDataStoreDbContext context;
    private readonly IMapper mapper;
    public PendingApprovalQueryHandler(ApprovalFlowDataStoreDbContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<IList<ApprovalModel>> Handle(ApprovalsQuery request, CancellationToken cancellationToken)
    {
        List<ApprovalRequest> results;
        if (request.PendingOnly)
        {
            results = this.context.ApprovalRequests.AsSplitQuery().Include(req => req.PersonalIdentities).Include(req => req.Requests).ThenInclude(req => req.History).Where(req => req.Completed == null).ToList();

        }
        else
        {
            results = this.context.ApprovalRequests.AsSplitQuery().Include(req => req.PersonalIdentities).Include(req => req.Requests).ThenInclude(req => req.History).ToList();

        }


        if (results.Any())
        {
            Serilog.Log.Information($"Found {results.Count()} results");
            return this.mapper.Map<List<ApprovalModel>>(results);
        }
        else
        {
            return new List<ApprovalModel>();
        }
    }
}
