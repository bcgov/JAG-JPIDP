namespace jumwebapi.Infrastructure.Auth;

public static class Claims
{
    public const string Address = "address";
    public const string AssuranceLevel = "identity_assurance_level";
    public const string Birthdate = "birthdate";
    public const string Email = "email";
    public const string FamilyName = "family_name";
    public const string GivenName = "given_name";
    public const string GivenNames = "given_names";
    public const string IdentityProvider = "identity_provider";
    public const string PreferredUsername = "preferred_username";
    public const string ResourceAccess = "resource_access";
    public const string RealmAccess = "realm_access";
    public const string Subject = "sub";
}
public static class Policies
{
    public const string BcscAuthentication = "bcsc-authentication-policy";
    public const string IdirAuthentication = "idir-authentication-policy";
    public const string AnyPartyIdentityProvider = "party-idp-policy";
    public const string UserOwnsResource = "user-owns-resource-policy";
    public const string AllDemsIdentityProvider = "dems-idp-policy";
    public const string BcpsAuthentication = "bcps-authentication-policy";
}
public static class Resources
{
    public const string JumApi = "PIDP-SERVICE";
    public const string SAEforms = "SAT-EFORMS";
}