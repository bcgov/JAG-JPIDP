namespace Pidp.Features.Admin.SubmittingAgencies;

using Pidp.Models.Lookups;
using System.Threading.Tasks;
using AutoMapper;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Microsoft.EntityFrameworkCore;

public record SubmittingAgencyQuery() : IQuery<List<SubmittingAgency>>;

public class SubmittingAgencyQueryHandler : IQueryHandler<SubmittingAgencyQuery, List<SubmittingAgency>>
{

    private readonly IMapper mapper;
    private readonly PidpDbContext context;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IHttpContextAccessor httpContextAccessor;

    public SubmittingAgencyQueryHandler(IMapper mapper, PidpDbContext context, IKeycloakAdministrationClient keycloakAdministrationClient, IHttpContextAccessor httpContextAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<SubmittingAgency>> HandleAsync(SubmittingAgencyQuery query)
    {
        return await this.context.SubmittingAgencies
          .ToListAsync();
    }
}
