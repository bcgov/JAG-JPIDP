namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Jum;

/// <summary>
/// Search for a case
/// </summary>
public class DigitalEvidenceCaseQuery
{

    public class Query : IQuery<Models.DigitalEvidenceCaseModel>
    {
        public string AgencyFileNumber { get; set; } = string.Empty;
        public string PartyId { get; set; } = string.Empty;
    }

    public class QueryHandler(IEdtCaseManagementClient client, IJumClient jumClient) : IQueryHandler<Query, Models.DigitalEvidenceCaseModel>
    {
        public async Task<Models.DigitalEvidenceCaseModel> HandleAsync(Query query)
        {
            var response = await client.FindCase(query.PartyId, query.AgencyFileNumber);

            if (response != null && response.Status == "NotFound")
            {
                Serilog.Log.Information($"[{query.PartyId}] Case {query.AgencyFileNumber} not found in EDT - checking for case in JUSTIN");
                var justinResponse = await jumClient.GetJustinCaseStatus(query.PartyId, query.AgencyFileNumber, "");

                if (justinResponse != null)
                {
                    response.JustinStatus = justinResponse;
                }
                Serilog.Log.Information($"JUSTIN case status for {query.AgencyFileNumber} is {justinResponse}");

            }

            if (response == null)
            {
                Serilog.Log.Warning($"[{query.PartyId}] Case {query.AgencyFileNumber} not found in EDT or JUSTIN");
            }

            return response;
        }
    }

}
