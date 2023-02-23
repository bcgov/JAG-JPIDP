namespace Pidp.Features.DigitalEvidenceCaseManagement.Query;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Models;

public class SubmittingAgency
{

    public sealed record Query(int RequestId) : IQuery<Model?>;
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.RequestId).GreaterThan(0);
    }
    public class QueryHandler : IQueryHandler<Query, Model?>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<Model?> HandleAsync(Query query)
        {
            var agencyRequestAttachements = await this.context.AgencyRequestAttachments
                .Where(request => request.SubmittingAgencyRequest.RequestId == query.RequestId)
                .Select(attachment => new ModelAttachment
                {
                    AttachmentName = attachment.AttachmentName,
                    AttachmentType = attachment.AttachmentType,
                    UploadStatus = attachment.UploadStatus
                }).ToListAsync(); //this can be removed, but an option to add attachment to sub-agency case access request
            return await this.context.SubmittingAgencyRequests
                .Where(access => access.RequestId == query.RequestId)
                .OrderByDescending(access => access.RequestedOn)
                .Select(access => new Model
                {
                    PartyId = access.PartyId,
                    CaseNumber = access.CaseNumber,
                    RequestedOn = access.RequestedOn,
                    AgencyCode = access.AgencyCode,
                    LastUpdated = access.Modified,
                    RequestStatus = access.RequestStatus,
                    AgencyRequestAttachments = agencyRequestAttachements
                })
                .SingleOrDefaultAsync();
        }
    }
    public class Model
    {
        public int PartyId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string AgencyCode { get; set; } = string.Empty;
        public Instant RequestedOn { get; set; }
        public Instant LastUpdated { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
        public ICollection<ModelAttachment> AgencyRequestAttachments { get; set; } = new List<ModelAttachment>();

    }
    public class ModelAttachment
    {
        public string AttachmentName { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
        public string UploadStatus { get; set; } = AgencyRequestStatus.Queued;
    }
}
