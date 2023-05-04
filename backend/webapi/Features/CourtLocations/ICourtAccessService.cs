namespace Pidp.Features.CourtLocations;

using Pidp.Models.Lookups;
using Pidp.Models;
using Pidp.Features.CourtLocations.Commands;

public interface ICourtAccessService
{
    public Task<bool> CreateAddCourtAccessDomainEvent(CourtLocationAccessRequest request);
    public Task<bool> CreateRemoveCourtAccessDomainEvent(CourtLocationAccessRequest request);

    public Task<CourtLocationAccessRequest> CreateCourtLocationRequest(CourtAccessRequest.Command command, CourtLocation location);
    public Task<List<CourtLocationAccessRequest>> GetRequestsDueToday();
    public Task<bool> DeleteAccessRequest(CourtLocationAccessRequest request);

}
