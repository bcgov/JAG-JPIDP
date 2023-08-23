namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Models;
using Prometheus;

public class DigitalEvidenceByRequestIdQuery
{

    private static readonly Counter RCCActiveQueryCount = Metrics
    .CreateCounter("case_rcc_active_user_query_count_total", "Number of requests to see RCC assigned users.");

    private static readonly Histogram RCCSearchDuration = Metrics
    .CreateHistogram("case_rcc_active_user_query_duration", "Histogram  of requests to see RCC assigned users.");

    public sealed record Query(int RequestId) : IQuery<DigitalEvidenceCaseModel?>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.RequestId).GreaterThan(0);
    }
    public class QueryHandler : IQueryHandler<Query, DigitalEvidenceCaseModel?>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<DigitalEvidenceCaseModel?> HandleAsync(Query query)
        {
            //var agencyRequestAttachments = await this.context.AgencyRequestAttachments
            //    .Where(request => request.SubmittingAgencyRequest.RequestId == query.RequestId)
            //    .Select(attachment => new ModelAttachment
            //    {
            //        AttachmentName = attachment.AttachmentName,
            //        AttachmentType = attachment.AttachmentType,
            //        UploadStatus = attachment.UploadStatus
            //    }).ToListAsync(); //this can be removed, but an option to add attachment to sub-agency case access request

            RCCActiveQueryCount.Inc();

            using (RCCSearchDuration.NewTimer())
            {

                return await this.context.SubmittingAgencyRequests
                .Where(access => access.RequestId == query.RequestId)
                .OrderByDescending(access => access.RequestedOn)
                .Select(access => new DigitalEvidenceCaseModel
                {
                    PartyId = access.PartyId,
                    Id = access.CaseId,
                    RequestId = query.RequestId,
                    AgencyFileNumber = access.AgencyFileNumber,
                    RequestedOn = access.RequestedOn,
                    LastUpdated = access.Modified,
                    RequestStatus = access.RequestStatus,
                })
                .SingleOrDefaultAsync();
            }
        }
    }


}
