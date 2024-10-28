namespace Pidp.Features.AccessRequests;

using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        private readonly ILogger<QueryHandler> logger;

        public QueryHandler(PidpDbContext context, ILogger<QueryHandler> logger)
        {
            this.context = context;
            this.logger = logger;

        }

        public async Task<PaginatedResponse<AccessRequestDTO>> HandleAsync(Query query)
        {
            var response = new PaginatedResponse<AccessRequestDTO>
            {
                Page = query.Input.Page,
                PageSize = query.Input.PageSize
            };

            if (query.Input.Filters != null && query.Input.Filters.Count > 0)
            {
                if (query.Input.Filters.TryGetValue("type", out var value) && value == "caseAccessRequests")
                {

                    this.logger.LogInformation("Case Access Request Search");
                    var agencyRequests = await this.context.SubmittingAgencyRequests.Include(a => a.Party)
                        .Skip((query.Input.Page - 1) * query.Input.PageSize)
                        .Take(query.Input.PageSize)
                        .ToListAsync();

                    response.Total = this.context.SubmittingAgencyRequests.Count();


                    var data = AccessRequestMappingService.MapToDTO(agencyRequests);


                    response.Data = data;
                }
            }
            else
            {
                this.logger.LogInformation("Access Request Search");

                var requests = await this.context.AccessRequests.Include(a => a.Party)
                    .Skip((query.Input.Page - 1) * query.Input.PageSize)
                    .Take(query.Input.PageSize)
                    .ToListAsync();

                response.Total = this.context.AccessRequests.Count();

                var data = AccessRequestMappingService.MapToDTO(requests);

                response.Data = data;
            }
            response.TotalPages = (int)Math.Ceiling((double)response.Total / query.Input.PageSize);
            return response;
        }

    }
}
