namespace Pidp.Features.DigitalEvidenceCaseManagement.Commands;

using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using Pidp.Data;
using Pidp.Features.AccessRequests;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;

public class SubmittingAgency
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public string SubmittingAgencyCode { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public string CaseGroup { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.PartyId).NotEmpty();
            this.RuleFor(x => x.CaseNumber).NotEmpty();
            this.RuleFor(x => x.CaseGroup).NotEmpty();
            this.RuleFor(x => x.SubmittingAgencyCode).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
        }
    }
    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        //private readonly HttpContext httpContext;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer;

        public CommandHandler(IClock clock, ILogger<CommandHandler> logger, PidpConfiguration config, PidpDbContext context, IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer)
        {
            this.clock = clock;
            this.logger = logger;
            this.config = config;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            //this.httpContext = httpContext;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            var dto = await this.GetPidpUser(command);

            //var userIdp = this.httpContext.User.GetIdentityProvider();

            //var subAgency = await this.context.Set<Models.Lookups.SubmittingAgency>()
            //        .Where(agencyIdp => agencyIdp.IdpHint == userIdp)
            //        .FirstOrDefaultAsync();

            if (!dto.AlreadyEnroled
                || dto.Email == null) //user must be already enroled i.e access to DEMS
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
                AggregateType = command.SubmittingAgencyCode,
                AggregateId = $"{command.PartyId}",
                EventType = subAgencyRequest.Created < this.clock.GetCurrentInstant() ? "Case.AccessRequest.Submitted" : "Case.AccessRequest.Updated",
                EventPayload = JsonConvert.SerializeObject(new SubAgencyDomainEvent
                {
                    RequestId = subAgencyRequest.RequestId,
                    CaseNumber = subAgencyRequest.CaseNumber,
                    PartyId = subAgencyRequest.PartyId,
                    AgencyCode = command.SubmittingAgencyCode,
                    CaseGroup = subAgencyRequest.CaseGroup,
                    Username = dto.Jpdid,
                    RequestedOn = subAgencyRequest.RequestedOn
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
                Username = dto.Jpdid,
                RequestedOn = subAgencyRequest.RequestedOn,
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
                RequestedOn = this.clock.GetCurrentInstant(),
                CaseGroup = command.CaseGroup,
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
}
