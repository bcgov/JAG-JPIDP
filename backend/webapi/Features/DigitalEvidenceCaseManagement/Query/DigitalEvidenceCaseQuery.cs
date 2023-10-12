namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using Pidp.Infrastructure.HttpClients.Edt;
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

        public QueryHandler(IEdtCaseManagementClient client) => this.client = client;

        public async Task<Models.DigitalEvidenceCaseModel> HandleAsync(Query query) => await this.client.FindCase(query.PartyId, query.AgencyFileNumber);
    }

}
