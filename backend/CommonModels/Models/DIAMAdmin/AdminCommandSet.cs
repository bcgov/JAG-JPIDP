namespace CommonModels.Models.DIAMAdmin;

public enum AdminCommandSet
{
    PING,
    PARTY_REMOVE_REQUEST,
    PARTY_ACCESS_REQUEST_RESET,
    PARTY_ACCESS_REQUEST_REMOVE,
    SSO_USER_GROUP_ADD,
    SSO_USER_GROUP_REMOVE,
    SSO_GROUP_ADD,
    SSO_GROUP_DELETE

}

public static class AdminCommandSetExtensions
{
    public static string ToSnakeCase(this AdminCommandSet commandSet) => commandSet.ToString().ToLower().Replace('_', '-');
}
