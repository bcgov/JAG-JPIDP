namespace Pidp.Features.Admin.CourtLocations;

using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;

using Pidp.Models.Lookups;


public record CourtLocationQuery(bool ActiveOnly) : IQuery<List<CourtLocation>>;

public class CourtLocationQueryHandler : IQueryHandler<CourtLocationQuery, List<CourtLocation>>
{

    private readonly IMapper mapper;
    private readonly PidpDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;

    public CourtLocationQueryHandler(IMapper mapper, PidpDbContext context,  IHttpContextAccessor httpContextAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<CourtLocation>> HandleAsync(CourtLocationQuery query)
    {
        if (query.ActiveOnly)
        {
            return await this.context.CourtLocations.Where(loc => loc.Active).OrderBy(loc => loc.Name).ToListAsync();
        }
        else
        {
            return await this.context.CourtLocations.OrderBy(loc => loc.Name).ToListAsync();
        }
    }
}
