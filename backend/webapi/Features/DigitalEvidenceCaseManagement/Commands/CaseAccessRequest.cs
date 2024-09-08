namespace Pidp.Features.DigitalEvidenceCaseManagement.Commands;

using Confluent.Kafka;
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
using Prometheus;

public class CaseAccessRequest
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public string SubmittingAgencyCode { get; set; } = string.Empty;
        public string AgencyFileNumber { get; set; } = string.Empty;
        public int RequestId { get; set; }
        public int CaseId { get; set; }
        public string? Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CaseGroup { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.AgencyFileNumber).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.CaseId).GreaterThan(0);
            // BCPSDEMS-1655 - case key not necessary
            // this.RuleFor(command => command.Key).NotEmpty();

        }
    }
    public class CommandHandler(IClock clock, ILogger<CommandHandler> logger,
        PidpConfiguration config, PidpDbContext context,
        IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer,
        IEdtCaseManagementClient caseMgmtClient
            ) : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer;
        private static readonly Histogram CaseQueueRequestDuration = Metrics
            .CreateHistogram("pipd_case_request_duration", "Histogram of case request call durations.");

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
            using (CaseQueueRequestDuration.NewTimer())
            {

                var dto = await this.GetPidpUser(command);

                if (!dto.AlreadyEnroled
                    || dto.Email == null) //user must be already enrolled i.e access to DEMS
                {
                    this.logger.LogUserNotEnroled(dto.Jpdid); //throw dems user not enrolled error
                    return DomainResult.Failed();
                }

                using var trx = this.context.Database.BeginTransaction();

                try
                {

                    if (string.IsNullOrEmpty(command.Key))
                    {
                        // case has no RCC number - we'll record and move on
                        this.logger.LogCaseMissingKey(command.CaseId, dto.Jpdid);
                    }

                    var subAgencyRequest = await this.SubmitAgencyCaseRequest(command); //save all trx at once for production(remove this and handle using idempotent)

                    // var exportedEvent = this.AddOutbox(command, subAgencyRequest, dto);

                    await this.PublishSubAgencyAccessRequest(dto, subAgencyRequest);

                        var addedRows = await context.SaveChangesAsync();
                        if (addedRows > 0)
                        {
                            this.logger.LogRequestCase(command.AgencyFileNumber, command.PartyId, subAgencyRequest.RequestId);
                            await trx.CommitAsync();

                            var publishedResponse = await this.PublishSubAgencyAccessRequest(dto, subAgencyRequest);
                            if (publishedResponse.Status != PersistenceStatus.Persisted)
                            {
                                this.logger.LogFailedToPublishCase(subAgencyRequest.RequestId, command.PartyId);
                                subAgencyRequest.RequestStatus = AgencyRequestStatus.Failed;
                                await context.SaveChangesAsync();
                                return DomainResult.Failed();
                            }
                        }
                        else
                        {
                            this.logger.LogDigitalEvidenceAccessTrxFailed($"Failed to store record for Request:{command.RequestId} Party:{command.PartyId} {command.AgencyFileNumber}");

                        }

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

        private Task<Models.OutBoxEvent.ExportedEvent> AddOutbox(Command command, SubmittingAgencyRequest subAgencyRequest, PartyDto dto)
        {
            var exportedEvent = this.context.ExportedEvents.Add(new Models.OutBoxEvent.ExportedEvent
            {
                AggregateType = $"SubmittingAgency.{command.SubmittingAgencyCode}",
                AggregateId = $"{subAgencyRequest.RequestId}",
                DateOccurred = this.clock.GetCurrentInstant(),
                EventType = subAgencyRequest.Created < this.clock.GetCurrentInstant() ? "CaseAccessRequestCreated" : "CaseAccessRequestUpdated",
                EventPayload = JsonConvert.SerializeObject(new SubAgencyDomainEvent
                {
                    RequestId = subAgencyRequest.RequestId,
                    CaseId = subAgencyRequest.CaseId,
                    PartyId = subAgencyRequest.PartyId,
                    Username = subAgencyRequest.Party!.Jpdid,
                    RequestedOn = subAgencyRequest.RequestedOn
                })
            });
            return Task.FromResult(exportedEvent.Entity);
        }

        private async Task<DeliveryResult<string, SubAgencyDomainEvent>> PublishSubAgencyAccessRequest(PartyDto dto, SubmittingAgencyRequest subAgencyRequest)
        {
            var msgKey = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Publishing Sub Agency Domain Event to topic {0} {1} {2}", this.config.KafkaCluster.CaseAccessRequestTopicName, msgKey, subAgencyRequest.RequestId);
            await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.CaseAccessRequestTopicName, msgKey, new SubAgencyDomainEvent
            {
                RequestId = subAgencyRequest.RequestId,
                CaseId = subAgencyRequest.CaseId,
                PartyId = subAgencyRequest.PartyId,
                EventType = CaseEventType.Provisioning,
                AgencyFileNumber = subAgencyRequest.AgencyFileNumber,
                Username = dto.Jpdid,
                UserId = dto.UserId,
                RequestedOn = subAgencyRequest.RequestedOn,
            });

            if (publishResponse.Status == Confluent.Kafka.PersistenceStatus.Persisted)
            {
                Serilog.Log.Logger.Information($"Published response to {publishResponse.TopicPartition} for {subAgencyRequest.RequestId}");
            }
            else
            {
                Serilog.Log.Logger.Error($"Failed to publish to {config.KafkaCluster.CaseAccessRequestTopicName} for {subAgencyRequest.RequestId}");
                throw new AccessRequestException($"Failed to publish to {config.KafkaCluster.CaseAccessRequestTopicName} for {subAgencyRequest.RequestId}");
            }

            return publishResponse;
        }

        private async Task<SubmittingAgencyRequest> SubmitAgencyCaseRequest(Command command)
        {

            var subAgencyAccessRequest = new SubmittingAgencyRequest
            {
                CaseId = command.CaseId,
                RequestStatus = AgencyRequestStatus.Queued,
                AgencyFileNumber = command.AgencyFileNumber,
                RCCNumber = command.Key,
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
}

public static partial class SubmittingAgencyLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Request for case access denied for party {partyId} and case {caseId}")]
    public static partial void LogSubmittingAgencyAccessRequestDenied(this ILogger logger, int partyId, int caseId);
    [LoggerMessage(2, LogLevel.Information, "Submitting Agency Case Access Request denied due to user {username} is not enroled to DEMS.")]
    public static partial void LogUserNotEnroled(this ILogger logger, string? username);
    [LoggerMessage(3, LogLevel.Warning, "Case request ID {caseId} - for user {username} does not have a valid key (RCCNumber).")]
    public static partial void LogCaseMissingKey(this ILogger logger, int caseId, string? username);
    [LoggerMessage(4, LogLevel.Information, "Tools case {caseId} request - for user {username}.")]
    public static partial void LogRequestToolsCase(this ILogger logger, int caseId, string? username);
    [LoggerMessage(5, LogLevel.Information, "Saved request {subAgencyRequestId} for {agencyFileNumber} party: {partyId}")]
    public static partial void LogRequestCase(this ILogger logger, string? agencyFileNumber, int partyId, int subAgencyRequestId);
    [LoggerMessage(6, LogLevel.Error, "Failed to publish case request for {subAgencyRequestId}: Party {partyId}")]
    public static partial void LogFailedToPublishCase(this ILogger logger, int subAgencyRequestId, int partyId);
}
