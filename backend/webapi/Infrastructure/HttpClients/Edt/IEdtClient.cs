namespace Pidp.Infrastructure.HttpClients.Edt;

using Pidp.Models;
public interface IEdtClient
{

    Task<DigitalEvidenceCaseModel?> FindCase(string caseName);

}
