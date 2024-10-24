namespace Common.Constants.Auth;

public static class Claims
{
    public const string Address = "address";
    public const string AssuranceLevel = "identity_assurance_level";
    public const string Birthdate = "birthdate";
    public const string Gender = "gender";
    public const string Email = "email";
    public const string FamilyName = "family_name";
    public const string GivenName = "given_name";
    public const string GivenNames = "given_names";
    public const string IdentityProvider = "identity_provider";
    public const string PreferredUsername = "preferred_username";
    public const string ResourceAccess = "resource_access";
    public const string Subject = "sub";
    public const string AuthorizedParties = "azp";
    public const string Roles = "roles";
    public const string BcPersonFamilyName = "BCPerID_last_name";
    public const string BcPersonGivenName = "BCPerID_first_name";
    public const string MembershipStatusCode = "membership_status_code";
}

public static class AccountTypes
{
    public const string EdtSaml2 = "SAML";
}

public static class DefaultRoles
{
    public const string Bcps = "BCPS";
}

public static class ClaimValues
{
    public const string BCServicesCard = "bcsc";
    public const string Idir = "idir";
    public const string Phsa = "phsa";
    public const string Bcps = "adfscert";
    public const string Adfs = "adfs"; // test
    public const string SubmittingAgency = "SUBMITTING_AGENCY";
    public const string VerifiedCredentials = "verified";
    public const string AzureAd = "azuread";
    public const string KeycloakUserPass = "keycloak-user-pass";
}

public static class Clients
{
    public const string PidpService = "PIDP-SERVICE";
    public const string AdminApi = "DIAM-BCPS-ADMIN";
    public const string DiamInternal = "DIAM-INTERNAL";


}

public static class Policies
{
    public const string BcscAuthentication = "bcsc-authentication-policy";
    public const string IdirAuthentication = "idir-authentication-policy";
    public const string AnyPartyIdentityProvider = "party-idp-policy";
    public const string SubAgencyIdentityProvider = "subgency-idp-policy";
    public const string UserOwnsResource = "user-owns-resource-policy";
    public const string VerifiedCredentialsProvider = "verified-credentials-authentication-policy";
    public const string AllJAMIdentityProvider = "jam-idp-policy";

    public const string AllDemsIdentityProvider = "dems-idp-policy";
    public const string AllDefenceIdentityProvider = "all-defense-idp-policy";
    public const string DefenceConselIdentityProvider = "defense-counsel-idp-policy";
    public const string DutyConselIdentityProvider = "duty-counsel-idp-policy";
    public const string BcpsAuthentication = "bcps-authentication-policy";
    public const string AdminAuthentication = "admin-authentication-policy";
    public const string AdminClientAuthentication = "admin-client-authentication-policy";
    public const string DiamInternalAuthentication = "diam-internal-authentication-policy";

    public const string ApprovalAuthorization = "approval-authentication-policy";

}



public static class Roles
{
    public const string Approver = "APPROVER";
    public const string ApprovalViewer = "APPROVAL-VIEWER";
    // PIdP Role Placeholders
    public const string Admin = "ADMIN";
    public const string User = "USER";
    public const string JAM_POR = "JAM_POR";

    // represents agencies that need upload access (e.g. police)
    public const string SubmittingAgency = "SUBMITTING_AGENCY";
    public const string SubmittingAgencyClient = "SUBMITTING_AGENCY_CLIENT";
    // clients lawyer
    public const string DefenceCounsel = "DEFENCE_COUNSEL";
    // legal aid appointed counsel
    public const string DutyCounsel = "DUTY_COUNSEL";

    public const string DisclosureAdmin = "DISCLOSURE_ADMIN";


}
