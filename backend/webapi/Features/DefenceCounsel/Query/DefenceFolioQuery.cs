namespace Pidp.Features.DefenceCounsel.Query;

using FluentValidation;
using Pidp.Models;

/// <summary>
/// Defence Counsel folios are effectively cases in DEMS
/// 
/// </summary>
public class DefenceFolioQuery
{

    public sealed record Query(string DefenceUniqueID) : IQuery<DigitalEvidenceCaseModel>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            this.RuleFor(x => x.DefenceUniqueID).NotEmpty();
        }
    }

    public class QueryHandler : IQueryHandler<Query, DigitalEvidenceCaseModel>
    {
        public async Task<DigitalEvidenceCaseModel> HandleAsync(Query query)
        {
            return new DigitalEvidenceCaseModel
            {
                
            };
        }
    }
}
