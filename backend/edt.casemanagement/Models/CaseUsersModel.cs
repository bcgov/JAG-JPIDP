namespace edt.casemanagement.Models;

public class CaseUsersModelx
{
    public List<CaseUserx> CaseUsers { get; set; }
}

public class CaseUserx
{
    public int CaseId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }

}
