namespace Pidp.Features.Parties;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FluentValidation;
using Pidp.Features.Organization.UserTypeService;
using Pidp.Models;

public class UserType
{
    public sealed record Query : IQuery<UserTypeModel?>
    {
        [Required]
        public int PartyId { get; set; }

        public Query(int partyId) => this.PartyId = partyId;

        public Query()
        {
        }
    }
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.PartyId).GreaterThan(0);
    }
    public class QueryHandler : IQueryHandler<Query, UserTypeModel?>
    {
        private readonly IUserTypeService userType;

        public QueryHandler(IUserTypeService userType) => this.userType = userType;

        public async Task<UserTypeModel?> HandleAsync(Query query) => await this.userType.GetOrgUserType(query.PartyId);
    }
}
