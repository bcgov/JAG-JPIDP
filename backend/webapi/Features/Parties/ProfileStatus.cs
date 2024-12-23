namespace Pidp.Features.Parties;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Constants.Auth;
using Common.Models.EDT;
using Common.Models.JUSTIN;
using CommonConstants.Constants.DIAM;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Extensions;
using Pidp.Infrastructure;
using Pidp.Infrastructure.HttpClients.Claims;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Infrastructure.HttpClients.Plr;
using Pidp.Models;
using Pidp.Models.Lookups;
using Pidp.Models.UserInfo;
using Prometheus;

public partial class ProfileStatus
{
    private readonly PidpConfiguration configuration;

    public ProfileStatus(PidpConfiguration configuration)
    {
        this.configuration = configuration;
    }
    private static readonly Histogram ProfileDuration = Metrics.CreateHistogram("pidp_profile_duration", "Histogram of profile duration requests.");


    public class Command : ICommand<Model>
    {
        [Required]
        public int Id { get; set; }
        [JsonIgnore]
        public ClaimsPrincipal? User { get; set; }
        public Command WithUser(ClaimsPrincipal user)
        {
            this.User = user;
            return this;
        }
    }

    public partial class Model
    {
        [JsonConverter(typeof(PolymorphicDictionarySerializer<string, ProfileSection>))]
        public Dictionary<string, ProfileSection> Status { get; set; } = new();
        public HashSet<Alert> Alerts => new(this.Status.SelectMany(x => x.Value.Alerts));

        public abstract class ProfileSection
        {
            internal abstract string SectionName { get; }
            public HashSet<Alert> Alerts { get; set; } = new();
            public StatusCode StatusCode { get; set; }
            public double Order { get; set; }

            public bool IsComplete => this.StatusCode is StatusCode.Complete or StatusCode.LockedComplete;

            public ProfileSection(ProfileStatusDto profile) => this.SetAlertsAndStatus(profile);

            protected abstract void SetAlertsAndStatus(ProfileStatusDto profile);
        }



        public enum Alert
        {
            TransientError = 1,
            PlrBadStanding,
            JumValidationError,
            PendingRequest,
            LawyerStatusError,
            PersonVerificationError,
            VerifiedCredentialMismatch
        }

        public enum StatusCode
        {
            Incomplete = 1,
            Complete,
            Locked,
            Error,
            Hidden,
            Available,
            Pending,
            HiddenComplete,   // not shown the in UI but completed
            LockedComplete,    // shown in the UI but not editable
            PriorStepRequired,
            RequiresApproval,
            Approved,
            Denied,
            MissingRequiredClaims
        }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator() => this.RuleFor(x => x.Id).GreaterThan(0);
    }

    public class CommandHandler : ICommandHandler<Command, Model>
    {
        private const string SUBAGENCY = "subagency";
        private readonly IMapper mapper;
        private readonly IPlrClient client;
        private readonly IJumClient jumClient;
        private readonly PidpConfiguration pidpConfiguration;
        private readonly PidpDbContext context;
        private readonly IJUSTINClaimClient justinClaimClient;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IProfileUpdateService profileUpdateService;

        public CommandHandler(
            IMapper mapper,
            IPlrClient client,
            IJUSTINClaimClient justinClaimClient,
            IJumClient jumClient,
            PidpDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IProfileUpdateService profileUpdateService,
            PidpConfiguration configuration)
        {
            this.mapper = mapper;
            this.client = client;
            this.context = context;
            this.jumClient = jumClient;
            this.httpContextAccessor = httpContextAccessor;
            this.justinClaimClient = justinClaimClient;
            this.profileUpdateService = profileUpdateService;
            this.pidpConfiguration = configuration;
        }

        public async Task<Model> HandleAsync(Command command)
        {

            using (ProfileDuration.NewTimer())
            {
                var party = await this.context.Parties.Where(party => party.Id == command.Id).SingleAsync();

                var profile = await this.context.Parties
                    .Where(party => party.Id == command.Id)
                    .ProjectTo<ProfileStatusDto>(this.mapper.ConfigurationProvider)
                    .SingleAsync();

                // if user if a lawyer they may update email and phone number
                if (profile.OrganizationDetailEntered && profile.Organization?.IdpHint == ClaimValues.VerifiedCredentials)
                {

                    await this.profileUpdateService.UpdateUserProfile(new UpdatePersonContactInfoModel
                    {
                        PartyId = party.Id,
                        EMail = party.Email,
                        Phone = party.Phone,
                        KeycloakUserId = party.UserId.ToString(),
                        Key = party.Jpdid,
                        Idp = profile.Organization.IdpHint,
                        Organization = profile.Organization.Name
                    });

                }

                var orgCorrectionDetail = profile.OrganizationCode == OrganizationCode.CorrectionService
                    ? await this.context.CorrectionServiceDetails
                    .Include(cor => cor.CorrectionService)
                    .Where(detail => detail.OrgainizationDetail.Id == profile.OrgDetailId)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync()
                    : null;



                var orgJusticeSecDetail = profile.OrganizationCode == OrganizationCode.JusticeSector
                    ? await this.context.JusticeSectorDetails
                    .Include(jus => jus.JusticeSector)
                    .Where(detail => detail.OrgainizationDetail.Id == profile.OrgDetailId)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync()
                    : null;


                // if the user is a BCPS user then we'll flag this portion as completed
                if (profile.OrganizationDetailEntered && profile.OrganizationCode == OrganizationCode.CorrectionService && orgCorrectionDetail != null)
                {
                    //get user token
                    var httpContext = this.httpContextAccessor.HttpContext;
                    var accessToken = await httpContext!.GetTokenAsync("access_token");
                    profile.EmployeeIdentifier = orgCorrectionDetail.PeronalId;
                    profile.CorrectionServiceCode = orgCorrectionDetail.CorrectionServiceCode;
                    profile.CorrectionService = orgCorrectionDetail.CorrectionService?.Name;
                    //profile.Organization = profile.or
                    profile.JustinUser = await this.jumClient.GetJumUserByPartIdAsync(long.Parse(profile.EmployeeIdentifier, CultureInfo.InvariantCulture), accessToken!);
                    profile.IsJumUser = await this.jumClient.IsJumUser(profile.JustinUser, new Party
                    {
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        Email = profile.Email,
                        Birthdate = profile.Birthdate,
                        Gender = profile.Gender
                    });

                    var justinPartAltId = party.AlternateIds.FirstOrDefault(alt => alt.Name == "JUSTINParticipant");
                    if (justinPartAltId == null)
                    {
                        var participant = profile.JustinUser.participantDetails.FirstOrDefault();
                        if (participant != null)
                        {
                            Serilog.Log.Information($"Storing JUSTIN alt id for {party.Id} as {participant.partId}");
                            party.AlternateIds.Add(new PartyAlternateId
                            {
                                Name = DIAMConstants.JUSTINPARTICIPANTID,
                                Value = participant.partId,
                                Party = party
                            });
                            await this.context.SaveChangesAsync();

                        }
                    }
                }



                // if an agency account then we'll mark as complete to prevent any changes
                var submittingAgency = await this.GetSubmittingAgency(command.User);
                if (submittingAgency != null)
                {
                    profile.UserIsInSubmittingAgency = true;
                    profile.SubmittingAgency = submittingAgency;
                    profile.OrganizationCode = OrganizationCode.SubmittingAgency;
                }

                if (profile.OrganizationDetailEntered && profile.OrganizationCode == OrganizationCode.JusticeSector && orgJusticeSecDetail != null)
                {
                    var accessToken = await this.httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                    profile.EmployeeIdentifier = orgJusticeSecDetail.JustinUserId;
                    profile.JusticeSectorCode = orgJusticeSecDetail.JusticeSectorCode;
                    profile.JusticeSectorService = orgJusticeSecDetail.JusticeSector?.Name;
                    profile.JustinUser = await this.jumClient.GetJumUserAsync(profile.EmployeeIdentifier, accessToken: accessToken!.ToString());
                    profile.IsJumUser = await this.jumClient.IsJumUser(profile.JustinUser, new Party
                    {
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        Email = profile.Email,
                        Birthdate = profile.Birthdate,
                        Gender = profile.Gender
                    });
                }

                profile.User = command.User;

                // if the user is not a card user then we shouldnt need more profile info
                if (!profile.UserIsBcServicesCard)
                {
                    // get the party
                    if (party != null)
                    {
                        profile.Email = party.Email;
                    }
                }
                else
                {
                    var lookups = this.context.PartyUserTypes
                            .Include(p => p.UserTypeLookup)
                            .Where(p => p.PartyId == party.Id)
                            .Select(put => put.UserTypeLookup.Name).ToList();
                    profile.UserTypes = lookups;
                }




                var profileStatus = new Model
                {
                    Status = new List<Model.ProfileSection>
                {
                    new Model.AccessAdministrator(profile),
                    new Model.OrganizationDetails(profile),

                    new Model.Demographics(profile, this.pidpConfiguration.AllowUserPassTestAccounts),
                    new Model.JamPor(this.justinClaimClient, profile),
                    new Model.DigitalEvidence(profile),
                    new Model.DigitalEvidenceCaseManagement(profile),
                    new Model.DefenseAndDutyCounsel(profile),
                    //new Model.Uci(profile),
                    new Model.SubmittingAgencyCaseManagement(profile),
                }
                    .ToDictionary(section => section.SectionName, section => section)
                };

                // handle ordering
                this.OrderProfile(command.User, profileStatus);

                return profileStatus;
            }

        }

        private void OrderProfile(ClaimsPrincipal user, Model profileStatus)
        {

            var identityProvider = user.GetIdentityProvider();
            var agency = this.context.SubmittingAgencies.Where(agency => agency.IdpHint.Equals(identityProvider)).FirstOrDefault();

            var idpKey = (agency != null) ? SUBAGENCY : identityProvider;

            var processFlows = this.context.ProcessFlows.Include(flow => flow.ProcessSection).AsSplitQuery().Where(flow => flow.IdentityProvider == idpKey).OrderBy(flow => flow.Sequence).ToList();

            var order = 0.0;
            foreach (var status in profileStatus.Status)
            {
                var flow = processFlows.Find(flow => flow.ProcessSection.Name.Equals(status.Value.SectionName, StringComparison.OrdinalIgnoreCase));
                if (flow != null)
                {
                    status.Value.Order = flow.Sequence;
                    if (status.Value.Order > order)
                    {
                        order = status.Value.Order;
                    }
                }
                else
                {
                    status.Value.Order = ++order;
                }
            }
        }



        private async Task<SubmittingAgency> GetSubmittingAgency(ClaimsPrincipal user)
        {
            // create an instance of the Query class
            var query = new Lookups.Index.Query();

            // create an instance of the QueryHandler class
            var handler = new Pidp.Features.Lookups.Index.QueryHandler(this.context);

            // execute the query and get the result
            var result = handler.HandleAsync(query);

            // get the SubmittingAgencies list from the result
            var submittingAgencies = result.Result.SubmittingAgencies;

            var agency = submittingAgencies.Find(agency => agency.IdpHint.Equals(user.GetIdentityProvider(), StringComparison.OrdinalIgnoreCase));

            if (agency != null)
            {
                return agency;
            }

            return null;
        }


    }


    public class ProfileStatusDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly? Birthdate { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Cpn { get; set; }
        public LicenceDeclarationDto? LicenceDeclaration { get; set; }
        public string? AccessAdministratorEmail { get; set; }
        public CollegeCode? CollegeCode { get; set; }
        public string? LicenceNumber { get; set; }
        public string? Ipc { get; set; }
        public int? OrgDetailId { get; set; }
        public double Order { get; set; }
        public OrganizationCode? OrganizationCode { get; set; }
        public Organization? Organization { get; set; }
        public CorrectionServiceCode? CorrectionServiceCode { get; set; }
        public string? CorrectionService { get; set; }
        public JusticeSectorCode? JusticeSectorCode { get; set; }
        public SubmittingAgency? SubmittingAgency { get; set; }
        public PublicUserValidation? CodeValidations { get; set; }
        public string? JusticeSectorService { get; set; }
        public string? EmployeeIdentifier { get; set; }
        //public bool OrganizationDetailEntered { get; set; }
        public IEnumerable<AccessTypeCode> CompletedEnrolments { get; set; } = Enumerable.Empty<AccessTypeCode>();
        public IEnumerable<string> UserTypes { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> AccessRequestStatus { get; set; } = Enumerable.Empty<string>();


        // Resolved after projection
        public PlrStandingsDigest PlrStanding { get; set; } = default!;
        public ClaimsPrincipal? User { get; set; }

        public HttpContextAccessor? HttpContextAccessor { get; set; }

        public Participant? JustinUser { get; set; }
        public bool IsJumUser { get; set; }

        // Computed Properties
        [MemberNotNullWhen(true, nameof(Email), nameof(Phone))]
        public bool DemographicsEntered => this.User.GetIdentityProvider() is ClaimValues.Bcps or
                                            ClaimValues.Idir or
                                            ClaimValues.Adfs or
                                            ClaimValues.AzureAd or
                                            ClaimValues.BCServicesCard or
                                            ClaimValues.VerifiedCredentials ? this.Email != null : this.Email != null && this.Phone != null;
        //[MemberNotNullWhen(true, nameof(CollegeCode), nameof(LicenceNumber))]
        //public bool CollegeCertificationEntered => this.CollegeCode.HasValue && this.LicenceNumber != null;
        [MemberNotNullWhen(true, nameof(OrganizationCode), nameof(EmployeeIdentifier))]
        public bool OrganizationDetailEntered => this.OrganizationCode.HasValue || this.EmployeeIdentifier != null;
        [MemberNotNullWhen(true, nameof(Organization))]
        public string? OrgName => this.Organization?.Name;
        public bool UserIsBcServicesCard => this.User.GetIdentityProvider() == ClaimValues.BCServicesCard;
        //public bool UserIsPhsa => this.User.GetIdentityProvider() == ClaimValues.Phsa;
        //public bool UserIsBcps => this.User.GetIdentityProvider() == ClaimValues.Bcps;
        public bool UserIsBcps => this.User.GetIdentityProvider() == ClaimValues.Bcps && this.User?.Identity is ClaimsIdentity identity && identity.GetResourceAccessRoles(Clients.PidpService).Contains(DefaultRoles.Bcps) || (this.PermitIDIRDEMS() && (this.User.GetIdentityProvider() == ClaimValues.Idir || this.User.GetIdentityProvider() == ClaimValues.AzureAd));
        public bool UserIsIdir => this.User.GetIdentityProvider() == ClaimValues.Idir || this.User.GetIdentityProvider() == ClaimValues.AzureAd;
        public bool UserIsIdirCaseManagement => this.User.GetIdentityProvider() == ClaimValues.Idir && this.PermitIDIRDEMS() && this.User?.Identity is ClaimsIdentity identity && identity.GetResourceAccessRoles(Clients.PidpService).Contains(Roles.SubmittingAgency);
        public bool UserIsDutyCounsel => (this.User.GetIdentityProvider() == ClaimValues.VerifiedCredentials && this.User?.Identity is ClaimsIdentity identity && identity.GetResourceAccessRoles(Clients.PidpService).Contains(Roles.DutyCounsel))
                  || (this.PermitIDIRDEMS() && (this.User.GetIdentityProvider() == ClaimValues.Idir || this.User.GetIdentityProvider() == ClaimValues.AzureAd) && this.User?.Identity is ClaimsIdentity claimsIdentity && claimsIdentity.GetResourceAccessRoles(Clients.PidpService).Contains(Roles.DutyCounsel));

        public bool UserIsTestJAMAccount => this.User.GetIdentityProvider() == ClaimValues.KeycloakUserPass;


        public bool UserIsInLawSociety => this.User.GetIdentityProvider() == ClaimValues.VerifiedCredentials;

        public bool UserIsInSubmittingAgency;


        protected bool PermitIDIRDEMS()
        {
            var permitIDIRDemsAccess = Environment.GetEnvironmentVariable("PERMIT_IDIR_DEMS_ACCESS");
            if (permitIDIRDemsAccess != null && permitIDIRDemsAccess.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [MemberNotNullWhen(true, nameof(LicenceDeclaration))]
        public bool HasDeclaredLicence => this.LicenceDeclaration?.HasNoLicence == false;

        public class LicenceDeclarationDto
        {
            public CollegeCode? CollegeCode { get; set; }
            public string? LicenceNumber { get; set; }

            [MemberNotNullWhen(false, nameof(CollegeCode), nameof(LicenceNumber))]
            public bool HasNoLicence => this.CollegeCode == null || this.LicenceNumber == null;
        }


    }
}
