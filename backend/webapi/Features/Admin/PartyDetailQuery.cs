namespace Pidp.Features.Admin;

using System.Threading.Tasks;
using AutoMapper;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Infrastructure.Services;

public record PartyDetailQuery(int partyId) : IQuery<PartyModel>;
public class PartyDetailQueryHandler : IQueryHandler<PartyDetailQuery, PartyModel>
{
    private readonly IMapper mapper;
    private readonly PidpDbContext context;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IEdtService edtService;
    private readonly IJumClient jumClient;
    private readonly IHttpContextAccessor httpContextAccessor;


    public PartyDetailQueryHandler(IMapper mapper, PidpDbContext context, IKeycloakAdministrationClient keycloakAdministrationClient, IJumClient jumClient, IHttpContextAccessor httpContextAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.jumClient = jumClient;
        this.httpContextAccessor = httpContextAccessor; 
    }

    public async Task<PartyModel?> HandleAsync(PartyDetailQuery query)
    {
        var user = this.context.Parties.Where(party => party.Id == query.partyId).FirstOrDefault();
        var httpContext = this.httpContextAccessor.HttpContext;
        var accessToken = await httpContext!.GetTokenAsync("access_token");


        if (user != null)
        {
            var partyModel = new PartyModel
            {
                Created = user.Created,
                Email = user.Email,
                Username = user.Jpdid,
                FirstName = user.FirstName,
                LastName = user.LastName,
                KeycloakUserId = user.UserId
            };

            // get the keycloak user
            var keycloakUser = await this.keycloakAdministrationClient.GetUser(user.UserId);

            var client = await this.keycloakAdministrationClient.GetClient("PIDP-SERVICE");

            if (keycloakUser != null && client != null)
            {
                partyModel.Enabled = keycloakUser.Enabled == true;
                partyModel.IdentityProvider = keycloakUser.Attributes.GetValueOrDefault("identityProvider").FirstOrDefault();
                var roles = await this.keycloakAdministrationClient.GetUserClientRoles(partyModel.KeycloakUserId, Guid.Parse(client.Id));
                partyModel.Roles = roles.Select(role => role.Name).ToList();

            }
            else
            {
                partyModel.UserIssues.Add("keycloak-user-missing", "Keycloak user not found");
            }

            if (keycloakUser.Attributes.ContainsKey("partId"))
            {
                var partId = keycloakUser.Attributes.GetValueOrDefault("partId").FirstOrDefault();

                if (decimal.TryParse(partId, out var value))
                {
                    // check JUSTIN
                    var part = await this.jumClient.GetJumUserByPartIdAsync(value);

                    partyModel.ParticipantId = decimal.Parse(partId);
                    var participant = part?.participantDetails.FirstOrDefault();

                    if (participant != null)
                    {

                        var justinUserModel = new SystemUserModel
                        {
                            AccountType = "oidc",
                            Key = partId,
                            Roles = participant.GrantedRoles.Select((role) => role.role.ToString()).ToList(),
                            System = "JUSTIN",
                            Enabled = participant.GrantedRoles.Where((role) => role.role.ToString().Contains("JRS")).Any(),
                            Username = participant.partUserId,
                            Regions = participant.assignedAgencies.Select((agency) => agency.agencyName).ToList(),
                        };

                        partyModel.SystemsAccess.Add("JUSTIN", justinUserModel);
                    }
                }
                else
                {
                    Serilog.Log.Information($"Part id {partId} is not a valid JUSTIN partID");
                }

            }


            return partyModel;
        }
        else
        {
            return null;
        }

    }
}
