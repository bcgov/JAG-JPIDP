namespace ISLInterfaces.Features.CaseAccess;
public interface ICaseAccessService
{
    public Task<List<string>> GetCaseAccessUsersAsync(string rccNumber);
}
