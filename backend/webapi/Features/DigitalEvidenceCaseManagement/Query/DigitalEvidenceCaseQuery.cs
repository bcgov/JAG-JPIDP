namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Jum;

public class DigitalEvidenceCaseQuery
{

    public class Query : IQuery<Models.DigitalEvidenceCaseModel>
    {
        public string AgencyFileNumber { get; set; } = string.Empty;
        public string PartyId { get; set; } = string.Empty;
    }

    public class QueryHandler : IQueryHandler<Query, Models.DigitalEvidenceCaseModel>
    {
        private readonly IEdtCaseManagementClient client;
        private readonly IJumClient jumClient;

        public QueryHandler(IEdtCaseManagementClient client, IJumClient jumClient)
        {
            this.jumClient = jumClient;
            this.client = client;
        }

        public async Task<Models.DigitalEvidenceCaseModel> HandleAsync(Query query)
        {
            var response = await this.client.FindCase(query.PartyId, query.AgencyFileNumber);

            if (response != null && response.Status == "NotFound")
            {
                var justinResponse = await this.jumClient.GetJustinCaseStatus(query.PartyId, query.AgencyFileNumber, "");

                if (justinResponse != null)
                {
                    response.JustinStatus = justinResponse;
                }
                Serilog.Log.Information($"JUSTIN case status for {query.AgencyFileNumber} is {justinResponse}");

            }

            return response;
        }
    }

}
