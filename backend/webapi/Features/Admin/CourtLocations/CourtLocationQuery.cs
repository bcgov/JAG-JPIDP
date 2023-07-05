namespace Pidp.Features.Admin.CourtLocations;

using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Models.Lookups;


public record CourtLocationQuery(bool ActiveOnly, bool IncludeEdtDetails) : IQuery<List<CourtLocationAdminModel>>;

public class CourtLocationQueryHandler : IQueryHandler<CourtLocationQuery, List<CourtLocationAdminModel>>
{

    private readonly IMapper mapper;
    private readonly IEdtDisclosureClient edtDisclosureClient;
    private readonly PidpDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;

    public CourtLocationQueryHandler(IMapper mapper, IEdtDisclosureClient edtDisclosureClient, PidpDbContext context,  IHttpContextAccessor httpContextAccessor)
    {
        this.mapper = mapper;
        this.edtDisclosureClient = edtDisclosureClient;
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<CourtLocationAdminModel>> HandleAsync(CourtLocationQuery query)
    {
        var response = new List<CourtLocationAdminModel>();
        if (query.ActiveOnly)
        {
            return await this.EnhanceCourtLocations(await this.context.CourtLocations.Where(loc => loc.Active).OrderBy(loc => loc.Name).ToListAsync(), query);
        }
        else
        {
            return await this.EnhanceCourtLocations(await this.context.CourtLocations.OrderBy(loc => loc.Name).ToListAsync(), query);
        }
    }

    private async Task<List<CourtLocationAdminModel>> EnhanceCourtLocations(List<CourtLocation> locations, CourtLocationQuery query) {
        var response = new List<CourtLocationAdminModel>();

        foreach (var location in locations)
        {
            var model = this.mapper.Map<CourtLocation, CourtLocationAdminModel>(location);
            if (query.IncludeEdtDetails)
            {
                var edtInfo = await edtDisclosureClient.GetCaseModelByKey(model.Key);
                if ( edtInfo == null)
                {
                    model.Status = "Not Found";
                }
                else
                {
                    model.Status = edtInfo.Status;
                    model.EdtId = edtInfo.Id;
                }
            }
            response.Add(model);
        }
        return response;
    }
}
