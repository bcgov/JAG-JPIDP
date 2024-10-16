namespace Pidp.Features.AccessRequests;

using System.Collections.Generic;
using System.Linq;
using Common.Models.AccessRequests;
using Pidp.Models;

public class AccessRequestMappingService
{

    public static AccessRequestDTO MapToDTO(AccessRequest accessRequest)
    {
        return new AccessRequestDTO
        {
            Id = accessRequest.Id,
            RequestType = accessRequest.GetType().Name,
            RequestDate = accessRequest.RequestedOn.ToDateTimeUtc(),
            Status = accessRequest.Status,
            Requester = accessRequest.Party.Jpdid
        };
    }

    public static List<AccessRequestDTO> MapToDTO(List<AccessRequest> accessRequests) => accessRequests.Select(MapToDTO).ToList();
}
