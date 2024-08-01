namespace Pidp.Features.Admin.IdentityProviders;

using System.Threading.Tasks;
using AutoMapper;
using Keycloak.Net.Models.RealmsAdmin;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Keycloak;

public record IdentityProviderQuery() : IQuery<List<IdentityProvider>>;

public class IdentityProviderQueryHandler : IQueryHandler<IdentityProviderQuery, List<IdentityProvider>>
{

    private readonly IMapper mapper;
    private readonly PidpDbContext context;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IHttpContextAccessor httpContextAccessor;


    public IdentityProviderQueryHandler(IMapper mapper, PidpDbContext context, IKeycloakAdministrationClient keycloakAdministrationClient, IHttpContextAccessor httpContextAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<IdentityProvider>> HandleAsync(IdentityProviderQuery query)
    {
        var providers = await this.keycloakAdministrationClient.GetIdentityProviders(Common.Constants.Auth.RealmConstants.BCPSRealm);
        return providers.ToList();
    }
}

