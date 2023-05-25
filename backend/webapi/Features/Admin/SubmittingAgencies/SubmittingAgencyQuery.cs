namespace Pidp.Features.Admin.SubmittingAgencies;

using Pidp.Models.Lookups;
using System.Threading.Tasks;
using AutoMapper;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Microsoft.EntityFrameworkCore;
using Pidp.Models;
using AutoMapper.QueryableExtensions;

public record SubmittingAgencyQuery() : IQuery<List<SubmittingAgencyModel>>;

public class SubmittingAgencyQueryHandler : IQueryHandler<SubmittingAgencyQuery, List<SubmittingAgencyModel>>
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

    public async Task<List<SubmittingAgencyModel>> HandleAsync(SubmittingAgencyQuery query)
    {
        var responseList = await this.context.SubmittingAgencies.ProjectTo<SubmittingAgencyModel>(this.mapper.ConfigurationProvider)
          .ToListAsync();

        foreach (var response in responseList)
        {
            if (!string.IsNullOrEmpty(response.IdpHint))
            {
                var provider = await this.keycloakAdministrationClient.GetIdentityProvider(response.IdpHint);
                if (provider != null)
                {
                    response.HasIdentityProvider = true;
                }


            }
        }

        return responseList;
    }
}
