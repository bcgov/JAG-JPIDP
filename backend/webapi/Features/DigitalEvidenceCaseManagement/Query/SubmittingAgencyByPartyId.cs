namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;

public class SubmittingAgencyByPartyId
{
    public sealed record Query(int PartyId) : IQuery<List<Model>>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.PartyId).NotEmpty();
        }
    }
    public class QueryHandler : IQueryHandler<Query, List<Model>>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<Model>> HandleAsync(Query query)
        {
            return await this.context.SubmittingAgencyRequests
                .Where(request => request.PartyId == query.PartyId)
                .Select(request => new Model
                {
                    AgencyFileNo = $"{request.AgencyCode}:{request.CaseNumber}",
                    RequestedOn = request.RequestedOn,
                    RequestStatus = request.RequestStatus,
                    CaseName = request.CaseName,
                })
                .ToListAsync();
        }
    }
    public class Model
    {
        public string AgencyFileNo { get; set; } = string.Empty;
        public Instant RequestedOn { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
        public string CaseName { get; set; } = string.Empty;
    }
}