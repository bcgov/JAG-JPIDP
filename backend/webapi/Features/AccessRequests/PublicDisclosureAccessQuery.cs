namespace Pidp.Features.AccessRequests;

using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

using Pidp.Data;
using Pidp.Models;

public class PublicDisclosureAccessQuery
{
    public class Query : IQuery<List<PublicDisclosureAccess>>
    {
        [Required]
        public int PartyId { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.PartyId).GreaterThan(0);
    }

    public class QueryHandler : IQueryHandler<Query, List<PublicDisclosureAccess>>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<PublicDisclosureAccess>> HandleAsync(Query query)
        {

            return await this.context.DigitalEvidencePublicDisclosures
          .Where(access => access.PartyId == query.PartyId)
          .OrderByDescending(access => access.RequestedOn)
          .Select(access => new PublicDisclosureAccess
          {
              KeyData = access.KeyData,
              Created = access.Created,
              RequestStatus = access.Status,
              CompletedOn = (access.Status == "Complete") ? access.Modified : null

          })
          .ToListAsync();
        }

    }
}
