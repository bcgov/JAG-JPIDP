namespace Pidp.Features.AccessRequests;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Common.Models.AccessRequests;
using CommonModels.Models.Web;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;

public class AccessRequestSearchQuery()
{
    public class Query : IQuery<PaginatedResponse<AccessRequestDTO>>
    {
        [Required]
        public PaginationInput Input { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {

    }
    public class QueryHandler : IQueryHandler<Query, PaginatedResponse<AccessRequestDTO>>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<PaginatedResponse<AccessRequestDTO>> HandleAsync(Query query)
        {

            var requests = await this.context.AccessRequests.Include(a => a.Party).ToListAsync();
            var paginatedResponse = nameof(PaginatedResponse<AccessRequestDTO>);
            var data = AccessRequestMappingService.MapToDTO(requests);

            return new PaginatedResponse<AccessRequestDTO>
            {
                Data = data,
                Page = query.Input.Page,
                PageSize = query.Input.PageSize,
                Total = data.Count
            };

        }

    }
}
