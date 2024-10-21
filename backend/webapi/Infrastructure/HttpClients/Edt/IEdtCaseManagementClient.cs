namespace Pidp.Infrastructure.HttpClients.Edt;

using Pidp.Models;
public interface IEdtCaseManagementClient
{

    Task<DigitalEvidenceCaseModel?> FindCase(string partyId, string caseName, string? rCCNumber);
    Task<DigitalEvidenceCaseModel?> GetCase(int caseId);

}
