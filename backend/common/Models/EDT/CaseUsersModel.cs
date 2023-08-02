namespace Common.Models.EDT;

public class CaseUsersModel
{
    public List<CaseUser> CaseUsers { get; set; }
}

public class CaseUser
{
    public int CaseId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }

}
