namespace Pidp.Features.Admin.AccessRequests;

using System.Threading.Tasks;
using Common.Models.AccessRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;
using Pidp.Data;

public record AccessRequestQuery(string UserName) : IQuery<List<AccessRequestDTO>>;

public class AccessRequestQueryHandler(PidpDbContext context, ILogger<AccessRequestQueryHandler> logger) : IQueryHandler<AccessRequestQuery, List<AccessRequestDTO>>
{

    public async Task<List<AccessRequestDTO>> HandleAsync(AccessRequestQuery query)
    {
        logger.LogInformation($"Access request for {query.UserName}");

        var accessRequests = await context.AccessRequests
              .Include(request => request.Party) // Include the Party property
              .Where(request => request.Party != null && request.Party.Jpdid == query.UserName)
              .ToListAsync();

        var accessRequestDTOs = new List<AccessRequestDTO>();

        foreach (var request in accessRequests)
        {
            accessRequestDTOs.Add(new AccessRequestDTO
            {
                Id = request.Id,
                Requester = request.Party!.Jpdid,
                RequestDate = request.RequestedOn.ToDateTimeUtc().ToLocalTime(),
                Status = request.Status,
                RequestType = request.AccessTypeCode.GetDisplayName(),
            });


        }

        var agencyRequests = await context.SubmittingAgencyRequests
        .Include(request => request.Party) // Include the Party property
        .Where(request => request.Party != null && request.Party.Jpdid == query.UserName)
        .ToListAsync();

        foreach (var request in agencyRequests)
        {
            accessRequestDTOs.Add(new AccessRequestDTO
            {
                Id = request.RequestId,
                Requester = request.Party!.Jpdid,
                RequestDate = request.RequestedOn.ToDateTimeUtc().ToLocalTime(),
                Status = request.RequestStatus,
                RequestType = "agency",
                RequestData = new Dictionary<string, string>
                {
                    { "AgencyFileNumber", request.AgencyFileNumber }
                }
            });


        }


        return accessRequestDTOs;
    }
}
