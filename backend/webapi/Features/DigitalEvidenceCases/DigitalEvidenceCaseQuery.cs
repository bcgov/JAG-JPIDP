namespace Pidp.Features.DigitalEvidenceCases;

using Pidp.Infrastructure.HttpClients.Edt;
public class DigitalEvidenceCaseQuery
{

    public class Query : IQuery<Models.DigitalEvidenceCaseModel>
    {
        public string CaseName { get; set; } = string.Empty;
    }

    public class QueryHandler : IQueryHandler<Query, Models.DigitalEvidenceCaseModel>
    {
        private readonly IEdtClient client;

        public QueryHandler(IEdtClient client) => this.client = client;

        public async Task<Models.DigitalEvidenceCaseModel> HandleAsync(Query query) => await this.client.FindCase(query.CaseName);
    }

}
