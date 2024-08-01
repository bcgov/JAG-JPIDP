namespace Pidp.Features.DigitalEvidenceCaseManagement.Commands;

using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Exceptions;
using Pidp.Features.AccessRequests;
using Pidp.Infrastructure.HttpClients.Edt;
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
        public bool ToolsCaseRequest { get; set; }
        public string? Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CaseGroup { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.AgencyFileNumber).NotEmpty().When(x => !x.ToolsCaseRequest);
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.CaseId).GreaterThan(0).When(x => !x.ToolsCaseRequest);
            // BCPSDEMS-1655 - case key not necessary
            // this.RuleFor(command => command.Key).NotEmpty();

        }
    }
    public class CommandHandler(IClock clock, ILogger<CaseAccessRequest.CommandHandler> logger,
        PidpConfiguration config, PidpDbContext context,
        IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer,
        IEdtCaseManagementClient caseMgmtClient
            ) : ICommandHandler<Command, IDomainResult>
    {
        private readonly ILogger logger = logger;
        private static readonly Histogram CaseQueueRequestDuration = Metrics
            .CreateHistogram("pipd_case_request_duration", "Histogram of case request call durations.");

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

                using (var trx = context.Database.BeginTransaction())
                {
                    try
                    {
                        // not a tools request and no key provided
                        if (!command.ToolsCaseRequest && string.IsNullOrEmpty(command.Key))
                        {
                            // case has no RCC number - we'll record and move on
                            this.logger.LogCaseMissingKey(command.CaseId, dto.Jpdid);
                        }

                        if (command.ToolsCaseRequest)
                        {
                            var toolsCase = await caseMgmtClient.GetCase(config.AUFToolsCaseId);
                            if (toolsCase == null)
                            {
                                throw new AccessRequestException("Tools case not found");
                            }
                            else
                            {
                                this.logger.LogRequestToolsCase(toolsCase.Id, dto.Jpdid);
                                command.Key = toolsCase.Key;
                                command.CaseId = toolsCase.Id;
                                command.Name = toolsCase.Name;
                                command.AgencyFileNumber = "AUF Tools Case";
                            }
                        }

                        var subAgencyRequest = await this.SubmitAgencyCaseRequest(command);


                        var addedRows = await context.SaveChangesAsync();
                        if (addedRows > 0)
                        {
                            this.logger.LogRequestCase(command.AgencyFileNumber, command.PartyId, subAgencyRequest.RequestId);
                            await trx.CommitAsync();

                            await this.PublishSubAgencyAccessRequest(dto, subAgencyRequest);
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
                }
                return DomainResult.Success();

            }
        }


        private async Task PublishSubAgencyAccessRequest(PartyDto dto, SubmittingAgencyRequest subAgencyRequest)
        {
            var msgKey = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Publishing Sub Agency Domain Event to topic {0} {1} {2}", config.KafkaCluster.CaseAccessRequestTopicName, msgKey, subAgencyRequest.RequestId);
            var publishResponse = await kafkaProducer.ProduceAsync(config.KafkaCluster.CaseAccessRequestTopicName, msgKey, new SubAgencyDomainEvent
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
                RequestedOn = clock.GetCurrentInstant()
            };

            context.SubmittingAgencyRequests.Add(subAgencyAccessRequest);

            return subAgencyAccessRequest;
        }

        private async Task<PartyDto> GetPidpUser(Command command)
        {
            return await context.Parties
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
}
