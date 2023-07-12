namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Models;

public class SubmittingAgencyByCaseId
{
    public sealed record Query(string RCCNumber) : IQuery<List<string>>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.RCCNumber).NotEmpty();
    }
    public class QueryHandler : IQueryHandler<Query, List<string>>
    {
        private readonly PidpDbContext context;
        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<string>> HandleAsync(Query query)
        {


            var result = await this.context.SubmittingAgencyRequests
              .Where(access => access.RCCNumber == query.RCCNumber && (access.RequestStatus == AgencyRequestStatus.Complete || access.RequestStatus == AccessRequestStatus.Pending))
              .Include(party => party.Party)
              .OrderBy(access => access.RequestedOn)
                            .Select(access => access.Party.Jpdid)
                            .ToListAsync();

            return result;


        }
    }
    public class Model
    {
        public List<ActiveSubmission> ActiveSubmissions { get; set; } = new List<ActiveSubmission>();

        public class ActiveSubmission
        {
            public List<string> ActiveUsers { get; set; } = new List<string>();
        }
    }
}


