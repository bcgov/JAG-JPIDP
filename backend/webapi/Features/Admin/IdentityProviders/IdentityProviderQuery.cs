namespace Pidp.Features.Admin.IdentityProviders;

using System.Threading.Tasks;
using AutoMapper;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Infrastructure.Services;

public record IdentityProviderQuery() : IQuery<IEnumerable<IdentityProviderModel>>;

public class IdentityProviderQueryHandler : IQueryHandler<IdentityProviderQuery, IEnumerable<IdentityProviderModel>>
{

    private readonly IMapper mapper;
    private readonly PidpDbContext context;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IEdtService edtService;
    private readonly IJumClient jumClient;
    private readonly IHttpContextAccessor httpContextAccessor;


    public IdentityProviderQueryHandler(IMapper mapper, PidpDbContext context, IKeycloakAdministrationClient keycloakAdministrationClient, IHttpContextAccessor httpContextAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.jumClient = jumClient;
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task<IEnumerable<IdentityProviderModel>> HandleAsync(IdentityProviderQuery query)
    {
        return null;
    }
}

