namespace Pidp.Features.Parties;

using System.Security.Claims;
using Pidp.Extensions;
using Pidp.Models;
using Pidp.Models.Lookups;
using Prometheus;
using Serilog;

public partial class ProfileStatus
{
    private static readonly char[] Separators = [' ', '-'];

    public static bool PermitIDIRCaseManagement()
    {
        var permitIDIRCaseMgmt = Environment.GetEnvironmentVariable("PERMIT_IDIR_CASE_MANAGEMENT");
        if (permitIDIRCaseMgmt != null && permitIDIRCaseMgmt.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public partial class Model
    {
        public class AccessAdministrator : ProfileSection
        {
            internal override string SectionName => "administratorInfo";
            public string? Email { get; set; }

            public AccessAdministrator(ProfileStatusDto profile) : base(profile) => this.Email = profile.AccessAdministratorEmail;

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!(profile.UserIsBcServicesCard))
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                this.StatusCode = string.IsNullOrWhiteSpace(profile.AccessAdministratorEmail)
                    ? StatusCode.Incomplete
                    : StatusCode.Complete;
            }
        }

        public class CollegeCertification : ProfileSection
        {
            internal override string SectionName => "collegeCertification";
            public bool HasCpn { get; set; }
            public bool LicenceDeclared { get; set; }

            public CollegeCertification(ProfileStatusDto profile) : base(profile)
            {
                this.HasCpn = !string.IsNullOrWhiteSpace(profile.Cpn);
                this.LicenceDeclared = profile.HasDeclaredLicence;
            }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (!profile.DemographicsEntered)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }

                if (profile.LicenceDeclaration == null)
                {
                    this.StatusCode = StatusCode.Incomplete;
                    return;
                }

                if (profile.LicenceDeclaration.HasNoLicence
                    || profile.PlrStanding.HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (profile.PlrStanding.Error)
                {
                    this.Alerts.Add(Alert.TransientError);
                    this.StatusCode = StatusCode.Error;
                    return;
                }

                this.Alerts.Add(Alert.PlrBadStanding);
                this.StatusCode = StatusCode.Error;
            }
        }

        public class Demographics : ProfileSection
        {
            internal override string SectionName => "demographics";
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public DateOnly? Birthdate { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public IEnumerable<string> UserTypes { get; set; }

            public Demographics(ProfileStatusDto profile) : base(profile)
            {
                this.FirstName = profile.FirstName;
                this.LastName = profile.LastName;
                this.Birthdate = profile.Birthdate;
                this.Email = profile.Email;
                this.Phone = profile.Phone;
                this.UserTypes = profile.UserTypes;
            }

            // submitting agency user details are locked
            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                this.StatusCode = profile.UserIsBcServicesCard || profile.UserIsIdir ? StatusCode.LockedComplete : profile.DemographicsEntered || profile.SubmittingAgency != null ?
                    (profile.SubmittingAgency != null || profile.UserIsBcps) ? StatusCode.HiddenComplete : StatusCode.Complete :

                    StatusCode.Incomplete;
            }
        }

        public class OrganizationDetails : ProfileSection
        {
            internal override string SectionName => "organizationDetails";
            public OrganizationCode? OrganizationCode { get; set; }
            public HealthAuthorityCode? HealthAuthorityCode { get; set; }
            public JusticeSectorCode? JusticeSectorCode { get; set; }
            public CorrectionServiceCode? CorrectionServiceCode { get; set; }
            public SubmittingAgency? SubmittingAgency { get; set; }
            public bool LawSociety { get; set; }
            public string? EmployeeIdentifier { get; set; }
            public string? orgName { get; set; }
            public string? CorrectionService { get; set; }
            private readonly Counter VcCredMismatchCounter = Metrics.CreateCounter("vc_cred_mismatch_total", "VC Credentials Mismatch count");

            public OrganizationDetails(ProfileStatusDto profile) : base(profile)
            {
                this.OrganizationCode = profile.OrganizationCode;
                this.EmployeeIdentifier = profile.EmployeeIdentifier;
                this.JusticeSectorCode = profile.JusticeSectorCode;
                this.CorrectionServiceCode = profile.CorrectionServiceCode;
                this.SubmittingAgency = profile.SubmittingAgency;
                this.orgName = profile.OrgName;
                this.CorrectionService = profile.CorrectionService;
                this.LawSociety = profile.UserIsInLawSociety;
            }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {

                if (profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.HiddenComplete;
                    return;
                }

                if (!(profile.UserIsBcps || profile.UserIsInSubmittingAgency || profile.UserIsInLawSociety))
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                // user is from an authenticated agency - no need to enter organization details or view/change them
                if (profile.UserIsInLawSociety)
                {

                    var bCServicesCardLastName = profile.User?.Claims.FirstOrDefault(claim => claim.Type.Equals("BCPerID_last_name", StringComparison.OrdinalIgnoreCase));
                    var bCServicesCardFirstName = profile.User?.Claims.FirstOrDefault(claim => claim.Type.Equals("BCPerID_first_name", StringComparison.OrdinalIgnoreCase));
                    var familyName = profile.User?.Claims.FirstOrDefault(claim => claim.Type.Equals("family_name", StringComparison.OrdinalIgnoreCase));
                    var givenName = profile.User?.Claims.FirstOrDefault(claim => claim.Type.Equals("given_name", StringComparison.OrdinalIgnoreCase));

                    if (bCServicesCardLastName == null || bCServicesCardFirstName == null || familyName == null || givenName == null)
                    {
                        Log.Warning($"VC Credential missing for {profile.User.GetUserId()} [{familyName}] [{bCServicesCardLastName}] [{givenName}] [{bCServicesCardFirstName}]");
                        this.Alerts.Add(Alert.VerifiedCredentialMismatch);
                        this.StatusCode = StatusCode.Error;
                        return;
                    }

                    // check the names match - we wont prevent moving ahead but will warn in the UI
                    var namesMatch = ValidateNames(bCServicesCardLastName, familyName, bCServicesCardFirstName, givenName);


                    if (!namesMatch)
                    {
                        Log.Warning($"VC Credential mismatch for {profile.User.GetUserId()} [{familyName}] [{bCServicesCardLastName}] [{givenName}] [{bCServicesCardFirstName}]");
                        this.Alerts.Add(Alert.VerifiedCredentialMismatch);
                        this.VcCredMismatchCounter.Inc();
                        this.StatusCode = StatusCode.Error;
                    }

                    var standing = profile.User?.Claims.FirstOrDefault(claim => claim.Type.Equals("membership_status_code", StringComparison.OrdinalIgnoreCase));
                    if (standing == null || standing.Value != "PRAC")
                    {
                        if (standing == null)
                        {
                            Log.Warning($"Lawyer membership code is null for user {profile.User.GetUserId()}");
                        }
                        else
                        {
                            Log.Warning($"Lawyer membership code is not set to PRAC {profile.User.GetUserId()} [{standing.Value}]");
                        }
                        this.Alerts.Add(Alert.LawyerStatusError);
                        this.StatusCode = StatusCode.Error;
                    }
                    else
                    {
                        // check user info is valid
                        this.StatusCode = StatusCode.HiddenComplete;
                    }

                    return;

                }

                // no org for bc services card users currently
                if (profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;

                }

                if (profile.UserIsInSubmittingAgency)
                {
                    this.StatusCode = StatusCode.HiddenComplete;
                    return;
                }

                if (!profile.DemographicsEntered)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }
                if (!profile.OrganizationDetailEntered)
                {
                    this.StatusCode = StatusCode.Incomplete;
                    return;
                }


                if (!profile.IsJumUser && !profile.UserIsInSubmittingAgency && profile.OrganizationDetailEntered)
                {
                    this.Alerts.Add(Alert.JumValidationError);
                    this.StatusCode = StatusCode.Error;
                    return;
                }




                this.StatusCode = StatusCode.Complete;
            }

            private static bool ValidateNames(Claim bCServicesCardLastName, Claim familyName, Claim bCServicesCardFirstName, Claim givenName)
            {
                var valid = true;
                var bcLastNames = bCServicesCardLastName.Value.Split(Separators, StringSplitOptions.TrimEntries);
                var familyNames = familyName.Value.Split(Separators, StringSplitOptions.TrimEntries);
                var bcFirstNames = bCServicesCardFirstName.Value.Split(Separators, StringSplitOptions.TrimEntries);
                var givenNames = givenName.Value.Split(Separators, StringSplitOptions.TrimEntries);

                if (!string.Equals(bcLastNames[0].Trim(), familyNames[0].Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($"User family name does not match between BCSC [{string.Join(" ", familyNames)}] and BCLaw [{string.Join(" ", bcLastNames)}]");
                    valid = false;
                }

                if (!string.Equals(bcFirstNames[0].Trim(), givenNames[0].Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($"User family name does not match between BCSC [{string.Join(" ", bcFirstNames)}] and BCLaw [{string.Join(" ", givenNames)}]");
                    valid = false;
                }

                return valid;
            }
        }

        public class JamPor : ProfileSection
        {
            internal override string SectionName => "jamPor";
            public JamPor(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsIdir)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }
                this.StatusCode = StatusCode.Available;

                if (profile.AccessRequestStatus.Any())
                {
                    var request = profile.AccessRequestStatus.First();
                    if (request != null)
                    {
                        this.StatusCode = Enum.Parse<StatusCode>(request);
                    }

                }


            }
        }

        public class DigitalEvidence : ProfileSection
        {
            internal override string SectionName => "digitalEvidence";

            public DigitalEvidence(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!(profile.UserIsBcServicesCard || profile.UserIsBcps || profile.UserIsInSubmittingAgency || profile.UserIsInLawSociety))
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (profile.UserIsBcServicesCard)
                {
                    // see if they have exceeded number of code attempts
                    var attempts = profile.CodeValidations;
                }

                // if a lawyer then they'll have two access requests pending (disclosure and core)
                if (profile.UserIsInLawSociety)
                {
                    var requests = profile.AccessRequestStatus.Distinct().ToList();
                    this.StatusCode = StatusCode.Available;
                    if (requests != null && requests.Count > 1)
                    {
                        foreach (var request in requests)
                        {
                            // if any request is errored then this is an error status
                            if (request.Equals(StatusCode.Error.ToString(), StringComparison.Ordinal))
                            {
                                Log.Information($"One or more events resulted in an error for {profile.User.GetUserId()}");
                                this.StatusCode = StatusCode.Error;
                                return;
                            }
                            if (request.Equals(StatusCode.Pending.ToString(), StringComparison.Ordinal))
                            {
                                this.StatusCode = StatusCode.Pending;
                                return;
                            }
                            if (request.Equals(StatusCode.RequiresApproval.ToString(), StringComparison.Ordinal))
                            {
                                this.StatusCode = StatusCode.RequiresApproval;
                                return;
                            }
                        }
                    }
                    else if (requests != null && requests.Count > 0)
                    {
                        this.StatusCode = Enum.Parse<StatusCode>(requests.First());
                    }
                    return;

                }

                if (profile.AccessRequestStatus.Contains(AccessRequestStatus.Pending))
                {
                    this.StatusCode = StatusCode.Pending;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.DigitalEvidence))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (!profile.IsJumUser && !profile.UserIsInSubmittingAgency && !profile.UserIsInLawSociety && profile.OrganizationDetailEntered)
                {
                    // cannot continue as prior step is incomplete
                    this.StatusCode = StatusCode.PriorStepRequired;
                    return;
                }

                if (!profile.UserIsInLawSociety && !profile.UserIsInSubmittingAgency && !profile.DemographicsEntered
                    || !profile.OrganizationDetailEntered && !profile.UserIsBcServicesCard
                    )
                {
                    this.StatusCode = StatusCode.PriorStepRequired;
                    return;
                }

                this.StatusCode = StatusCode.Available;
            }
        }
        public class SubmittingAgencyCaseManagement : ProfileSection
        {
            internal override string SectionName => "submittingAgencyCaseManagement";

            public SubmittingAgencyCaseManagement(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsInSubmittingAgency)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (!profile.DemographicsEntered
                    || !profile.CompletedEnrolments.Contains(AccessTypeCode.DigitalEvidence)
                    || !profile.OrganizationDetailEntered
                    || !profile.PlrStanding.HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }
            }
        }

        public class DefenseAndDutyCounsel : ProfileSection
        {
            internal override string SectionName => "digitalEvidenceCounsel";

            public DefenseAndDutyCounsel(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {

                if (profile.UserIsDutyCounsel)
                {
                    if (profile.CompletedEnrolments.Contains(AccessTypeCode.DigitalEvidence))
                    {
                        this.StatusCode = StatusCode.Available;
                        return;
                    }
                    else
                    {
                        this.StatusCode = StatusCode.PriorStepRequired;
                        return;
                    }
                }
                else
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }
            }
        }

        public class DigitalEvidenceCaseManagement : ProfileSection
        {
            internal override string SectionName => "digitalEvidenceCaseManagement";

            public DigitalEvidenceCaseManagement(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                // special testing case where IDIR users could view case management and request access as an SA
                if (profile.UserIsIdirCaseManagement)
                {
                    if (profile.CompletedEnrolments.Contains(AccessTypeCode.DigitalEvidence))
                    {
                        this.StatusCode = StatusCode.Complete;
                        return;
                    }
                    else
                    {
                        this.StatusCode = StatusCode.Locked;
                        return;
                    }
                }

                if (!profile.UserIsInSubmittingAgency)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }


                if (profile.CompletedEnrolments.Contains(AccessTypeCode.DigitalEvidence))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (!profile.DemographicsEntered
                    || !profile.OrganizationDetailEntered
                    || !profile.PlrStanding.HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }

                this.StatusCode = StatusCode.Pending;
            }
        }

        public class DriverFitness : ProfileSection
        {
            internal override string SectionName => "driverFitness";

            public DriverFitness(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.DriverFitness))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (!profile.DemographicsEntered
                    || !profile.PlrStanding.HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }

                this.StatusCode = StatusCode.Incomplete;
            }
        }

        public class HcimAccountTransfer : ProfileSection
        {
            internal override string SectionName => "hcimAccountTransfer";

            public HcimAccountTransfer(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                // TODO revert [
                // if (profile.CompletedEnrolments.Contains(AccessTypeCode.HcimAccountTransfer)
                //    || profile.CompletedEnrolments.Contains(AccessTypeCode.HcimEnrolment))
                // {
                //     this.StatusCode = StatusCode.Hidden;
                //     return;
                // }
                if (profile.CompletedEnrolments.Contains(AccessTypeCode.HcimAccountTransfer))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.HcimEnrolment))
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }
                // ]

                this.StatusCode = profile.DemographicsEntered
                    ? StatusCode.Incomplete
                    : StatusCode.Locked;
            }
        }

        public class HcimEnrolment : ProfileSection
        {
            internal override string SectionName => "hcimEnrolment";

            public HcimEnrolment(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                // TODO revert [
                // if (profile.CompletedEnrolments.Contains(AccessTypeCode.HcimAccountTransfer)
                //     || profile.CompletedEnrolments.Contains(AccessTypeCode.HcimEnrolment))
                // {
                //     this.StatusCode = StatusCode.Complete;
                //     return;
                // }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.HcimAccountTransfer))
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.HcimEnrolment))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }
                // ]

                this.StatusCode = !profile.DemographicsEntered || string.IsNullOrWhiteSpace(profile.AccessAdministratorEmail)
                    ? StatusCode.Locked
                    : StatusCode.Incomplete;
            }
        }

        public class MSTeams : ProfileSection
        {
            internal override string SectionName => "msTeams";

            public MSTeams(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.MSTeams))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (!profile.DemographicsEntered
                    || !profile.PlrStanding
                        .With(AccessRequests.MSTeams.AllowedIdentifierTypes)
                        .HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }

                this.StatusCode = StatusCode.Incomplete;
            }
        }

        public class SAEforms : ProfileSection
        {
            internal override string SectionName => "saEforms";

            public bool IncorrectLicenceType { get; set; }

            public SAEforms(ProfileStatusDto profile) : base(profile)
            {
                this.IncorrectLicenceType = profile.PlrStanding == null || profile.PlrStanding.HasGoodStanding
                    && !profile.PlrStanding
                        .Excluding(AccessRequests.SAEforms.ExcludedIdentifierTypes)
                        .HasGoodStanding;
            }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.SAEforms))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (!profile.DemographicsEntered
                    || !profile.PlrStanding
                        .Excluding(AccessRequests.SAEforms.ExcludedIdentifierTypes)
                        .HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }

                this.StatusCode = StatusCode.Incomplete;
            }
        }

        public class Uci : ProfileSection
        {
            internal override string SectionName => "uci";

            public Uci(ProfileStatusDto profile) : base(profile) { }

            protected override void SetAlertsAndStatus(ProfileStatusDto profile)
            {
                if (!profile.UserIsBcServicesCard)
                {
                    this.StatusCode = StatusCode.Hidden;
                    return;
                }

                if (profile.CompletedEnrolments.Contains(AccessTypeCode.Uci))
                {
                    this.StatusCode = StatusCode.Complete;
                    return;
                }

                if (!profile.DemographicsEntered
                    || !profile.PlrStanding.HasGoodStanding)
                {
                    this.StatusCode = StatusCode.Locked;
                    return;
                }

                this.StatusCode = StatusCode.Incomplete;
            }
        }
    }


}
