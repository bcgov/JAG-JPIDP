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
                .Select(party => new DigitalEvidenceCaseModel
                {
                    PartyId = party.PartyId,
                    Id = party.CaseId,
                    AgencyFileNumber = party.AgencyFileNumber,
                    RequestedOn = party.RequestedOn,
                    LastUpdated = party.Modified,
                    RequestStatus = party.RequestStatus,
                })
                .ToListAsync();
        }
    }

}
