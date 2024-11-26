namespace Pidp.Infrastructure.Services;

using System.Threading.Tasks;
using CommonModels.Models.DIAMAdmin;
using Pidp.Data;

public class DIAMAdminService(ILogger<DIAMAdminService> logger, PidpDbContext dbContext) : IDIAMAdminService
{
    public async Task<bool> ProcessAdminRequestAsync(AdminRequestModel request)
    {

        if (request == null || string.IsNullOrEmpty(request.Requestor) || request.RequestType == null)
        {
            return false;
        }


        switch (request.RequestType)
        {
            case AdminCommandSet.PING:
            {
                logger.LogInformation($"Ping request from {request.Requestor} - sending response");
                request.RequestData.Add("pong", Environment.MachineName);

                break;
            }
            case AdminCommandSet.PARTY_REMOVE_REQUEST:
            {
                var partyIdStr = request.RequestData["partyId"];
                if (string.IsNullOrEmpty(partyIdStr))
                {
                    logger.LogError($"No party in remove request {request}");
                    return false;
                }

                // convert partyid to int
                if (!int.TryParse(partyIdStr, out int partyId))
                {
                    logger.LogError($"Invalid party id in remove request {request}");
                    return false;
                }


                // get the party
                var party = dbContext.Parties.FirstOrDefault(p => p.Id == partyId);

                if (party == null)
                {
                    logger.LogError($"Party not found in remove request {request} - marking complete");
                    return true;
                }

                logger.LogInformation($"Processing remove-party-request for {request.RequestData["partyId"]} [{party.Jpdid}] from [{request.Requestor}]");

                // delete from the part table
                dbContext.Parties.Remove(party);
                await dbContext.SaveChangesAsync();
                break;
            }
            default:
            {
                logger.LogError($"Unknown admin request type {request.RequestType}");
                return false;
            }
        }
        return true;
    }
}
