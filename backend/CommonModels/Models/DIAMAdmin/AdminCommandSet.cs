namespace CommonModels.Models.DIAMAdmin;

using System.ComponentModel;

public enum AdminCommandSet
{
    [Description("Ping known Kafka instances")]
    PING,

    [Description("Create new application")]
    CREATE_NEW_APPLICATION,

    [Description("Remove a party (user) from DIAM")]
    PARTY_REMOVE_REQUEST,

    [Description("Resend an access request")]
    PARTY_ACCESS_REQUEST_RESET,

    [Description("Remove an access request")]
    PARTY_ACCESS_REQUEST_REMOVE,

    [Description("Add a user to a Keycloak group")]
    SSO_USER_GROUP_ADD,

    [Description("Remove a user from a Keycloak group")]
    SSO_USER_GROUP_REMOVE,

    [Description("Create new Keycloak group")]
    SSO_GROUP_ADD,

    [Description("Delete a Keycloak group")]
    SSO_GROUP_DELETE,

    [Description("Add a child group to an existing group")]
    SSO_GROUP_CHILD_ADD,

    [Description("Add roles to a Keycloak client")]
    SSO_GROUP_CLIENT_ROLES_ADD,

    [Description("Remove roles from a Keycloak client")]
    SSO_GROUP_CLIENT_ROLES_REMOVE,

    [Description("Create new Keycloak client")]
    SSO_CLIENT_ADD,

    [Description("Update an existing Keycloak client")]
    SSO_CLIENT_UPDATE


}

public static class AdminCommandSetExtensions
{
    public static string ToSnakeCase(this AdminCommandSet commandSet) => commandSet.ToString().ToLower().Replace('_', '-');
}

public static class ProjectTypeExtensions
{
    public static string GetDescription(this ProjectType projectType)
    {
        var field = projectType.GetType().GetField(projectType.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute == null ? projectType.ToString() : attribute.Description;
    }
}

