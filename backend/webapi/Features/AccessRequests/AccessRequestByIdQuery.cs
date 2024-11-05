namespace Pidp.Features.AccessRequests;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Common.Models.AccessRequests;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;

public class AccessRequestByIdQuery()
{
    public class Query : IQuery<AccessRequestDTO>
    {
        [Required]
        public int AccessRequestId { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {

    }
    public class QueryHandler : IQueryHandler<Query, AccessRequestDTO>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<AccessRequestDTO> HandleAsync(Query query)
        {

            var request = await this.context.AccessRequests.Include(req => req.Party).FirstOrDefaultAsync(req => req.Id == query.AccessRequestId);


            return AccessRequestMappingService.MapToDTO(request);

        }

    }
}
