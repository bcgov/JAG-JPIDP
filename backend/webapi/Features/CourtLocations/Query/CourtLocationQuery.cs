namespace Pidp.Features.CourtLocations.Query;

using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Models.Lookups;

public class CourtLocationQuery
{

    public class Query : IQuery<List<CourtLocation>>
    {
        public int PartyId { get; set; }
        // todo allow for types/city etc..

    }

    public class QueryHandler : IQueryHandler<Query, List<CourtLocation>>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<CourtLocation>> HandleAsync(Query query) => await this.context.Set<CourtLocation>().Where( loc => loc.Active).OrderBy( loc => loc.Name).ToListAsync();
    }

}
