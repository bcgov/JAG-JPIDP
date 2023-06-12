namespace Pidp.Infrastructure.HttpClients.Edt;

using Pidp.Models;

public interface IEdtDisclosureClient
{
    Task<DigitalEvidenceCaseModel?> FindFolio(int partyID, string folioID);

}
