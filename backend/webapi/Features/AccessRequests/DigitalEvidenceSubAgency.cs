namespace Pidp.Features.AccessRequests;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using Pidp.Data;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;

public class DigitalEvidenceSubAgency
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public string SubmittingAgencyCode { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
    }
    public class Query : IQuery<List<Model>>
    {
        public int RequestId { get; set; }
    }
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.RequestId).GreaterThan(0);
    }
    public class QueryHandler : IQueryHandler<Query, List<Model>>
    {
        private readonly PidpDbContext context;

        public QueryHandler(PidpDbContext context) => this.context = context;

        public async Task<List<Model>> HandleAsync(Query query)
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
                .ToListAsync();
        }
    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.PartyId).NotEmpty();
            this.RuleFor(x => x.CaseNumber).NotEmpty();
            this.RuleFor(x => x.RequestStatus).NotEmpty();
            this.RuleFor(x => x.SubmittingAgencyCode).IsInEnum();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
        }
    }
    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer;

        public CommandHandler(IClock clock, ILogger<CommandHandler> logger, PidpConfiguration config, PidpDbContext context, IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer)
        {
            this.clock = clock;
            this.logger = logger;
            this.config = config;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            var dto = await this.GetPidpUser(command);

            if (dto.AlreadyEnroled
                || dto.Email == null)
            {
                this.logger.LogSubmittingAgencyAccessRequestDenied();
                return DomainResult.Failed();
            }

            using var trx = this.context.Database.BeginTransaction();

            try
            {

                var subAgencyRequest = await this.SubmitAgencyCaseRequest(command); //save all trx at once for production(remove this and handle using idempotent)

                var exportedEvent = this.AddOutbox(command, subAgencyRequest, dto);

                await this.PublishSubAgencyAccessRequest(dto, subAgencyRequest);

                await this.context.SaveChangesAsync();
                await trx.CommitAsync();
            }
            catch (Exception ex)
            {

                this.logger.LogDigitalEvidenceAccessTrxFailed(ex.Message.ToString());
                await trx.RollbackAsync();
                return DomainResult.Failed();
            }

            return DomainResult.Success();


        }

        private Task<Models.OutBoxEvent.ExportedEvent> AddOutbox(Command command, SubmittingAgencyRequest subAgencyRequest, PartyDto dto)
        {
            var exportedEvent = this.context.ExportedEvents.Add(new Models.OutBoxEvent.ExportedEvent
            {
                EventId = subAgencyRequest.RequestId,
                AggregateType = "SubmittingAgency",
                AggregateId = $"{command.PartyId}",
                EventType = subAgencyRequest.Created < this.clock.GetCurrentInstant() ? "Case.Request.Submitted" : "Case.Request.Updated",
                EventPayload = JsonConvert.SerializeObject(new SubAgencyDomainEvent
                {
                    RequestId = subAgencyRequest.RequestId,
                    CaseNumber = subAgencyRequest.CaseNumber,
                    PartyId = subAgencyRequest.PartyId,
                    AgencyCode = subAgencyRequest.AgencyCode,
                    CaseGroup = subAgencyRequest.CaseGroup,
                    Username = dto.Jpdid
                })
            });
            return Task.FromResult(exportedEvent.Entity);
        }

        private async Task PublishSubAgencyAccessRequest(PartyDto dto, SubmittingAgencyRequest subAgencyRequest)
        {
            Serilog.Log.Logger.Information("Publishing Sub Agency Domain Event to topic {0} {1}", this.config.KafkaCluster.SubAgencyTopicName, subAgencyRequest.RequestId);
            await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.SubAgencyTopicName, $"{subAgencyRequest.RequestId}", new SubAgencyDomainEvent
            {
                RequestId = subAgencyRequest.RequestId,
                CaseNumber = subAgencyRequest.CaseNumber,
                PartyId = subAgencyRequest.PartyId,
                AgencyCode = subAgencyRequest.AgencyCode,
                CaseGroup = subAgencyRequest.CaseGroup,
                Username = dto.Jpdid
            });
        }

        private async Task<SubmittingAgencyRequest> SubmitAgencyCaseRequest(Command command)
        {
            var subAgencyAccessRequest = new SubmittingAgencyRequest
            {
                CaseNumber = command.CaseNumber,
                RequestStatus = AgencyRequestStatus.Queued,
                AgencyCode = command.SubmittingAgencyCode,
                PartyId = command.PartyId,
                RequestedOn = this.clock.GetCurrentInstant()
            };
            this.context.SubmittingAgencyRequests.Add(subAgencyAccessRequest);

            await this.context.SaveChangesAsync();
            return subAgencyAccessRequest;
        }

        private async Task<PartyDto> GetPidpUser(Command command)
        {
            return await this.context.Parties
                .Where(party => party.Id == command.PartyId)
                .Select(party => new PartyDto
                {
                    AlreadyEnroled = party.AccessRequests.Any(request => request.AccessTypeCode == AccessTypeCode.DigitalEvidence),
                    Cpn = party.Cpn,
                    Jpdid = party.Jpdid,
                    UserId = party.UserId,
                    Email = party.Email,
                    FirstName = party.FirstName,
                    LastName = party.LastName,
                    Phone = party.Phone
                })
                .SingleAsync();
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
public static partial class SUbmittingAgencyLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Submitting Agency Case Access Request denied due to the Request Record not meeting all prerequisites.")]
    public static partial void LogSubmittingAgencyAccessRequestDenied(this ILogger logger);
}
