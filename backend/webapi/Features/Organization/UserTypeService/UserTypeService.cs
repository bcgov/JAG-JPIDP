namespace Pidp.Features.Organization.UserTypeService;

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Infrastructure.Auth;
using Pidp.Models;
using Pidp.Models.Lookups;

public class UserTypeService : IUserTypeService
{
    private const string PUBLIC_ORG = "Public";
    private readonly PidpDbContext context;
    public string? OrgUserType { get; set; }


    public UserTypeService(PidpDbContext context) => this.context = context;//this.OrgUserType = orgUserType;
    public async Task<UserTypeModel?> GetOrgUserType(int partyId)
    {

        var userType = new UserTypeModel();
        if (this.context.PartyOrgainizationDetails.ToListAsync().Result.Count == 0)
        {
            return null;
        }



        var orgCode = await this.context.PartyOrgainizationDetails
            .Include(p => p.Organization)
            .Include(p => p.Party).AsSplitQuery()
            .Where(p => p.PartyId == partyId)
            .FirstOrDefaultAsync();

        if (orgCode == null)
        {
            var party = this.context.Parties.First(p => p.Id == partyId);
            // this should get updated once we actually know the status of the user based on requests
            // however there is a current limitation and a user is only in one org :-(
            Serilog.Log.Information($"Org is null for {partyId} - must be Public user of undetermined type");
            return new UserTypeModel
            {
                ParticipantId = party.Jpdid,
                OrganizationType = PUBLIC_ORG,
                OrganizationName = PUBLIC_ORG
            };
        }

        Serilog.Log.Information("Org Id {0}", orgCode?.Id);

        if (orgCode?.OrganizationCode == OrganizationCode.CorrectionService)
        {
            var corr = await this.context.CorrectionServiceDetails.SingleOrDefaultAsync(detail => detail.OrgainizationDetail == orgCode) ?? throw new KeyNotFoundException($"username {orgCode} not found");
            var corrCode = corr.CorrectionServiceCode switch
            {
                CorrectionServiceCode.OutofCustody => "Out of Custody",
                CorrectionServiceCode.Incustody => "In Custody User",
                CorrectionServiceCode.Both => throw new NotImplementedException(),
                _ => null
            };
            if (corrCode is not null)
            {
                this.OrgUserType = corrCode;
                userType = new UserTypeModel
                {
                    OrganizationType = nameof(OrganizationCode.CorrectionService),
                    OrganizationName = corrCode,
                    ParticipantId = corr.PeronalId
                };
                //.Add(nameof(OrganizationCode.CorrectionService), new Dictionary<string, string> { { "OrganizationName", corrCode }, { "ParticipantId", corr.PeronalId } });
            }
        }
        else if (orgCode?.OrganizationCode == OrganizationCode.JusticeSector)
        {
            var jsector = await this.context.JusticeSectorDetails.SingleOrDefaultAsync(detail => detail.OrgainizationDetail == orgCode) ?? throw new KeyNotFoundException($"username {orgCode} not found");
            var jsCode = jsector.JusticeSectorCode switch
            {
                JusticeSectorCode.BCPS => "BC Prosecution Service",
                JusticeSectorCode.RSBC => throw new NotImplementedException(),
                _ => null
            };
            if (jsCode is not null)
            {
                this.OrgUserType = jsCode;
                userType = new UserTypeModel
                {
                    OrganizationType = nameof(OrganizationCode.JusticeSector),
                    OrganizationName = jsCode,
                    ParticipantId = jsector.ParticipantId
                };
                //userType.Add(nameof(OrganizationCode.JusticeSector), new Dictionary<string, string> { { "OrganizationName", jsCode },{"ParticipantId", jsector.JustinUserId } });
            }
        }
        // law society member using verified credentials
        else if (orgCode.Organization != null && orgCode.Organization.IdpHint == ClaimValues.VerifiedCredentials)
        {
            if (orgCode.Party != null)
            {
                userType = new UserTypeModel
                {
                    OrganizationType = nameof(OrganizationCode.LawSociety),
                    OrganizationName = orgCode.Organization.Name,
                    ParticipantId = orgCode.Party.Jpdid
                };
            }
        }
        else if (orgCode.Organization != null && !string.IsNullOrEmpty(orgCode.Organization.IdpHint))
        {

            // get the party

            // see if user is in a submitting agency
            // create an instance of the Query class
            var query = new Pidp.Features.Lookups.Index.Query();

            // create an instance of the QueryHandler class
            var handler = new Pidp.Features.Lookups.Index.QueryHandler(this.context);

            // execute the query and get the result
            var result = handler.HandleAsync(query);

            // get the SubmittingAgencies list from the result
            var submittingAgencies = result.Result.SubmittingAgencies;

            var agency = submittingAgencies.Find(agency => agency.IdpHint.Equals(orgCode?.Organization?.IdpHint));

            if (agency != null && orgCode.Party != null)
            {
                userType = new UserTypeModel
                {
                    OrganizationType = nameof(OrganizationCode.SubmittingAgency),
                    OrganizationName = orgCode.Organization.Name,
                    ParticipantId = orgCode.Party.Email,
                    IsSubmittingAgency = true,
                    SubmittingAgencyCode = agency.Code,
                };
            }

        }
        return userType is null ? throw new KeyNotFoundException() : userType;
    }
}
