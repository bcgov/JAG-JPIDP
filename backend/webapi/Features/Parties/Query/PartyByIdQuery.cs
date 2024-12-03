namespace Pidp.Features.Parties.Query;

using System.ComponentModel.DataAnnotations;
using CommonModels.Models.Party;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;

public class PartyByIdQuery()
{
    public class Query : IQuery<PartyDetailModel>
    {
        [Required]
        public int partyId { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.partyId).GreaterThan(0);

    }

    public class QueryHandler : IQueryHandler<Query, PartyDetailModel>
    {
        private readonly PidpDbContext context;
        private readonly ILogger<QueryHandler> logger;

        public QueryHandler(PidpDbContext context, ILogger<QueryHandler> logger)
        {
            this.context = context;
            this.logger = logger;

        }

        public async Task<PartyDetailModel> HandleAsync(Query query)
        {
            logger.LogInformation($"Party query by ID {query.partyId}");
            var party = await this.context.Parties.FirstOrDefaultAsync(p => p.Id == query.partyId);

            var data = PartyMappingService.MapToDTO(party);

            return data;
        }

    }

}
