namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Models;
using static Pidp.Features.DigitalEvidenceCaseManagement.Query.SubmittingAgencyByCaseId.Model;

public class SubmittingAgencyByCaseId
{
    public sealed record Query(string CaseNumber) : IQuery<List<Model>>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.CaseNumber).NotEmpty();
    }
    public class QueryHandler : IQueryHandler<Query, List<Model>>
    {
        private readonly PidpDbContext context;
        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<Model>> HandleAsync(Query query)
        {


            return await this.context.SubmittingAgencyRequests
              .Where(access => access.CaseNumber == query.CaseNumber && access.RequestStatus == AgencyRequestStatus.Approved)
              .Include(party => party.Party)
              .GroupBy(access => access.AgencyCode)
              .OrderBy(access => access.Key)
              .Select(request => new Model
              {
                  ActiveSubmissions = new List<ActiveSubmission>
                  {
                      new ActiveSubmission
                      {
                          AgencyCode= request.Key,
                          ActiveUsers = request.Select(user => user.Party!.Jpdid!).ToList(),
                      }
                  }
              })
            .ToListAsync();

        }
    }
    public class Model
    {
        public List<ActiveSubmission> ActiveSubmissions { get; set; } = new List<ActiveSubmission>();

        public class ActiveSubmission
        {
            public string AgencyCode { get; set; } = string.Empty;
            public List<string> ActiveUsers { get; set; } = new List<string>();
        }
    }
}


