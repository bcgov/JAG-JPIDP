namespace CommonModels.Models.DIAMAdmin;


public class AdminUserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool IsAppCreationPermitted { get; set; }
    public bool IsAppCreationApprovalRequired { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public List<ApplicationModel> ApplicationsAccess { get; set; } = [];
    public List<UserEnvironmentAccessModel> EnvironmentAccessModel { get; set; } = [];

}

public class AdminAuditUserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<UserAuditLogModel> AuditLogs { get; set; } = [];

}

public class UserEnvironmentAccessModel
{
    public string EnvironmentName { get; set; } = string.Empty;
    public List<UserClientAccessModel> ClientAccessList { get; set; } = [];
    public List<UserRealmAccessModel> RealmAccess { get; set; } = [];
}

public class UserAuditLogModel
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
}




public class UserClientAccessModel
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
}

public class UserRealmAccessModel
{
    public int Id { get; set; }
    public string RealmId { get; set; } = string.Empty;
}
