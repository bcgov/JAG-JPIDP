namespace edt.casemanagement.Models;

public class UserCaseGroup
{
    public string UserId { get; set; } = string.Empty;
    public int GroupUserId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int GroupId { get; set; }

    public override string ToString() => $"UserId: {this.UserId}, GroupUserId: {this.GroupUserId}, GroupName: {this.GroupName}, GroupId: {this.GroupId}";
}
