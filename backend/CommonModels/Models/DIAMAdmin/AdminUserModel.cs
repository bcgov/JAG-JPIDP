namespace CommonModels.Models.DIAMAdmin;


public class AdminUserModel
{
    public string Username { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public List<UserAccessModel> KeycloakClientAccess { get; set; } = [];
}

public class UserAccessModel
{
    public string KeycloakEnvionment { get; set; } = string.Empty;
    public List<string> KeycloakClients { get; set; } = [];
}
