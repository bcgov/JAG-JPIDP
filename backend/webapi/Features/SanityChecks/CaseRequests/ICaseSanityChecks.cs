namespace Pidp.Features.SanityChecks.CaseRequests;

public interface ICaseSanityChecks
{
    public Task<int> HandlePendingCases();
}
