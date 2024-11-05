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

    public static AccessRequestDTO MapToDTO(SubmittingAgencyRequest agencyRequest)
    {
        return new AccessRequestDTO
        {
            Id = agencyRequest.RequestId,
            RequestType = agencyRequest.GetType().Name,
            RequestDate = agencyRequest.RequestedOn.ToDateTimeUtc(),
            Status = agencyRequest.RequestStatus,
            Requester = agencyRequest.Party.Jpdid,
            RequestData = new Dictionary<string, string>
            {
                { "CaseId", agencyRequest.CaseId.ToString() },
                { "AgencyFileNumber", agencyRequest.AgencyFileNumber },
                { "RCCNumber", agencyRequest.RCCNumber },
                { "Details", agencyRequest.Details }
            }
        };
    }
    public static List<AccessRequestDTO> MapToDTO(List<SubmittingAgencyRequest> agencyRequests) => agencyRequests.Select(MapToDTO).ToList();

    public static List<AccessRequestDTO> MapToDTO(List<AccessRequest> accessRequests) => accessRequests.Select(MapToDTO).ToList();
}
