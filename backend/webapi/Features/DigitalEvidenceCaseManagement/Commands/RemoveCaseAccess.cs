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
using Prometheus;

public class RemoveCaseAccess
{
    public sealed record Command(int RequestId) : ICommand<IDomainResult>;
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.RequestId).NotEmpty();
            this.RuleFor(x => x.RequestId).GreaterThan(0);

        }
    }
    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly ILogger logger;
        private readonly IClock clock;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer;
        private static readonly Histogram CaseQueueRemovalDuration = Metrics
    .CreateHistogram("pipd_case_removal_request_duration", "Histogram of case removal call durations.");

        public CommandHandler(ILogger<CommandHandler> logger, IClock clock, PidpConfiguration config, PidpDbContext context, IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer)
        {
            this.logger = logger;
            this.config = config;
            this.clock = clock;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
        }
        public async Task<IDomainResult> HandleAsync(Command command)
        {

            using (CaseQueueRemovalDuration.NewTimer())
            {

                var subAgencyRequest = await this.context.SubmittingAgencyRequests
                .Where(request => request.RequestId == command.RequestId)
                .SingleAsync();

                var dto = await this.GetPidpUser(subAgencyRequest.PartyId);

                if (subAgencyRequest == null)
                {
                    return DomainResult.Failed();
                }
                using var trx = this.context.Database.BeginTransaction();

                try
                {

                    subAgencyRequest.RequestStatus = AgencyRequestStatus.RemovalPending;

                    await this.context.SaveChangesAsync();
                    var exportedEvent = this.AddOutbox(subAgencyRequest, dto);

                    await this.PublishSubAgencyAccessRemovalRequest(dto, subAgencyRequest);

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

        }


        private Task<Models.OutBoxEvent.ExportedEvent> AddOutbox(SubmittingAgencyRequest subAgencyRequest, PartyDto dto)
        {
            var exportedEvent = this.context.ExportedEvents.Add(new Models.OutBoxEvent.ExportedEvent
            {
                EventId = subAgencyRequest.RequestId,
                AggregateType = "DigitalEvidenceCaseManagement",
                AggregateId = $"{subAgencyRequest.PartyId}",
                EventType = "Case.AccessRequest.Deleted",
                EventPayload = JsonConvert.SerializeObject(new SubAgencyDomainEvent
                {
                    RequestId = subAgencyRequest.RequestId,
                    CaseId = subAgencyRequest.CaseId,
                    PartyId = subAgencyRequest.PartyId,
                    AgencyFileNumber = subAgencyRequest.AgencyFileNumber,
                    Username = dto.Jpdid,
                    UserId = dto.UserId,
                    RequestedOn = subAgencyRequest.RequestedOn
                })
            });
            return Task.FromResult(exportedEvent.Entity);
        }
        private async Task PublishSubAgencyAccessRemovalRequest(PartyDto dto, SubmittingAgencyRequest subAgencyRequest)
        {

            var msgKey = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Publishing Sub Agency Delete Domain Event to topic {0} {1} {2}", this.config.KafkaCluster.CaseAccessRequestTopicName, subAgencyRequest.RequestId, msgKey);


            await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.CaseAccessRequestTopicName, msgKey, new SubAgencyDomainEvent
            {
                RequestId = subAgencyRequest.RequestId,
                CaseId = subAgencyRequest.CaseId,
                PartyId = subAgencyRequest.PartyId,
                AgencyFileNumber = subAgencyRequest.AgencyFileNumber,
                EventType = CaseEventType.Decommission,
                Username = dto.Jpdid,
                UserId = dto.UserId,
                RequestedOn = subAgencyRequest.RequestedOn
            });
        }
        private async Task<PartyDto> GetPidpUser(int partyId)
        {
            return await this.context.Parties
                .Where(party => party.Id == partyId)
                .Select(party => new PartyDto
                {
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
