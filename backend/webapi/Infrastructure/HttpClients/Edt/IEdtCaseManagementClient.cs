namespace Pidp.Infrastructure.HttpClients.Edt;

using Pidp.Models;
public interface IEdtCaseManagementClient
{

    Task<DigitalEvidenceCaseModel?> FindCase(string caseName);
    Task<DigitalEvidenceCaseModel?> GetCase(int caseId);

}
