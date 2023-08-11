namespace ApprovalFlow.Features.Approvals;

using System.Threading;
using System.Threading.Tasks;
using ApprovalFlow.Data;
using AutoMapper;

using Common.Models.Approval;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record PendingApprovalsQuery() : IRequest<IList<ApprovalModel>>;

public class PendingApprovalQueryHandler : IRequestHandler<PendingApprovalsQuery, IList<ApprovalModel>>
{
    private readonly ApprovalFlowDataStoreDbContext context;
    private readonly IMapper mapper;
    public PendingApprovalQueryHandler(ApprovalFlowDataStoreDbContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<IList<ApprovalModel>> Handle(PendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var results = this.context.ApprovalRequests.Include(req => req.Requests).ThenInclude(req => req.History).Where(req => req.Completed == null).ToList();

        if ( results.Any())
        {
            Serilog.Log.Information($"Found {results.Count()} results");
            return mapper.Map<List<ApprovalModel>>(results);
        }
        else
        {
            return new List<ApprovalModel>();
        }
    }
}
