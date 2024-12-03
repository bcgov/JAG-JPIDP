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
            case AdminCommandSet.PARTY_ACCESS_REQUEST_REMOVE:
            {
                logger.LogInformation($"Request received {request}");


                var accessRequestIdStr = request.RequestData["accessRequestId"];
                if (string.IsNullOrEmpty(accessRequestIdStr))
                {
                    logger.LogError($"No access request ID in remove request {request}");
                    return false;
                }
                // convert accessRequestIdStr to int
                if (!int.TryParse(accessRequestIdStr, out int accessRequestId))
                {
                    logger.LogError($"Invalid access request id in remove request {request}");
                    return false;
                }

                // get the access request
                var accessRequest = dbContext.AccessRequests.FirstOrDefault(ar => ar.Id == accessRequestId);
                if (accessRequest == null)
                {
                    logger.LogError($"Access request not found in remove request {request} - marking complete");
                    return true;
                }
                else
                {
                    dbContext.AccessRequests.Remove(accessRequest);
                    await dbContext.SaveChangesAsync();
                    break;
                }

                break;
            }
            case AdminCommandSet.PARTY_REMOVE_REQUEST:
            {
                logger.LogInformation($"Request received {request}");

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
