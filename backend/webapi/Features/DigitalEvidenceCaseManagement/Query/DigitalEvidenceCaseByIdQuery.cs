namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Models;

public class DigitalEvidenceCaseByIdQuery
{
    public sealed record Query(int CaseId) : IQuery<DigitalEvidenceCaseModel?>;


    public class QueryHandler : IQueryHandler<Query, Models.DigitalEvidenceCaseModel>
    {
        private readonly IEdtCaseManagementClient client;

        public QueryHandler(IEdtCaseManagementClient client) => this.client = client;

        public async Task<Models.DigitalEvidenceCaseModel> HandleAsync(Query query) => await this.client.GetCase(query.CaseId);
    }
}
