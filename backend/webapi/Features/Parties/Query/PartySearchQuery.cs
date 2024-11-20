namespace Pidp.Features.Parties.Query;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommonModels.Models.Party;
using CommonModels.Models.Web;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;

public class PartySearchQuery()
{
    public class Query : IQuery<PaginatedResponse<PartyDetailModel>>
    {
        [Required]
        public PaginationInput Input { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {

    }

    public class QueryHandler : IQueryHandler<Query, PaginatedResponse<PartyDetailModel>>
    {
        private readonly PidpDbContext context;
        private readonly ILogger<QueryHandler> logger;

        public QueryHandler(PidpDbContext context, ILogger<QueryHandler> logger)
        {
            this.context = context;
            this.logger = logger;

        }

        public async Task<PaginatedResponse<PartyDetailModel>> HandleAsync(Query query)
        {
            var response = new PaginatedResponse<PartyDetailModel>
            {
                Page = query.Input.Page,
                PageSize = query.Input.PageSize
            };


            this.logger.LogInformation("Access Request Search");

            var requests = await this.context.Parties
                .Skip((query.Input.Page - 1) * query.Input.PageSize)
                .Take(query.Input.PageSize)
                .ToListAsync();

            response.Total = this.context.AccessRequests.Count();

            var data = PartyMappingService.MapToDTO(requests);

            response.Data = data;

            response.TotalPages = (int)Math.Ceiling((double)response.Total / query.Input.PageSize);
            return response;
        }

    }
}
