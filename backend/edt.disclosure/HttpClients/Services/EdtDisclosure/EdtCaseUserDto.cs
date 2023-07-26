namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

public class EdtCaseUserDto
{
    public int CaseUserId { get; set; }
    public int CaseId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<EdtGroupUserDto> Groups { get; set; }  = new List<EdtGroupUserDto>();
}
