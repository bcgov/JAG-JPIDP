namespace Pidp.Features.Parties;

using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Models;

public class CrownRegionQuery
{

    public sealed record Query : IQuery<IEnumerable<OrgUnitModel?>>
    {
        [Required]
        public required int PartyId { get; set; }
        [Required]
        public required decimal ParticipantId { get; set; }

        public Query(int partyId, decimal participantId)
        {
            this.PartyId = partyId;
            this.ParticipantId = participantId;
        }

        public Query() { }


    }
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            this.RuleFor(x => x.ParticipantId).GreaterThan(0);
            this.RuleFor(x => x.PartyId).GreaterThan(0);
        }


    }
    public class QueryHandler : IQueryHandler<Query, IEnumerable<OrgUnitModel?>>
    {
        private readonly IOrgUnitService orgUnitService;

        public QueryHandler(IOrgUnitService orgUnitService) => this.orgUnitService = orgUnitService;

        public async Task<IEnumerable<OrgUnitModel?>> HandleAsync(Query query) => await this.orgUnitService.GetUserOrgUnitGroup(query.PartyId, query.ParticipantId);
    }
}
