namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Models;

public class DigitalEvidenceByPartyQuery
{
    public sealed record Query(int PartyId) : IQuery<List<DigitalEvidenceCaseModel>>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.PartyId).NotEmpty();
        }
    }
    public class QueryHandler : IQueryHandler<Query, List<DigitalEvidenceCaseModel>>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<DigitalEvidenceCaseModel>> HandleAsync(Query query)
        {
            return await this.context.SubmittingAgencyRequests
                .Where(request => request.PartyId == query.PartyId)
                .Select(caseRequest => new DigitalEvidenceCaseModel
                {
                    Id = caseRequest.CaseId,
                    RequestId = caseRequest.RequestId,
                    AgencyFileNumber = caseRequest.AgencyFileNumber,
                    RequestedOn = caseRequest.RequestedOn,
                    LastUpdated = caseRequest.Modified,
                    RequestStatus = caseRequest.RequestStatus,
                })
                .ToListAsync();
        }
    }

}
