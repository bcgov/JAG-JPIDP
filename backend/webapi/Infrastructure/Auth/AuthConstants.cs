namespace Pidp.Infrastructure.Auth;

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

    public const string VerifiedCredPresentedRequestId = "pres_req_conf_id";

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
    public const string AzureAd = "azuread";
    public const string Bcps = "adfscert";
    public const string Adfs = "adfs"; // test
    public const string SubmittingAgency = "SUBMITTING_AGENCY";
    public const string VerifiedCredentials = "verified";
    public const string KeycloakUserPass = "keycloak-user-pass";

}

