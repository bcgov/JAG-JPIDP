namespace Pidp.Features.DefenceCounsel.Query;

using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Models;

/// <summary>
/// Defence Counsel folios are effectively cases in DEMS
/// 
/// </summary>
public class DefenceFolioQuery
{

    public sealed record Query : IQuery<DigitalEvidenceCaseModel>
    {
        [Required]
        public int PartyId { get; set; }
        [Required]
        public string DefenceUniqueID { get; set; }

        public Query(int partyId, string defenceUniqueID)
        {
            this.PartyId = partyId;
            this.DefenceUniqueID = defenceUniqueID;
        }

        public Query()
        {
        }
    }
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            this.RuleFor(x => x.DefenceUniqueID).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);

        }
    }

    public class QueryHandler : IQueryHandler<Query, DigitalEvidenceCaseModel>
    {
        private readonly IEdtDisclosureClient client;
        private readonly ILogger logger;

        public QueryHandler(IEdtDisclosureClient client, ILogger<DefenceFolioQuery> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task<DigitalEvidenceCaseModel?> HandleAsync(Query query)
        {
            var caseInfo = await this.client.FindFolio(query.PartyId, query.DefenceUniqueID);

            if (caseInfo == null)
            {
                this.logger.LogFolioNotFound(query.PartyId, query.DefenceUniqueID);
                return null;
            }

            return caseInfo;
        }
    }
}

public static partial class DefenceFolioQueryLogger
{
    [LoggerMessage(1, LogLevel.Warning, "Folio not found for party {partyId} and case {caseId}")]
    public static partial void LogFolioNotFound(this ILogger logger, int partyId, string caseId);

}

