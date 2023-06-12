namespace Pidp.Features.AccessRequests;

using DomainResults.Common;
using FluentValidation;
using NodaTime;
using Pidp.Data;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Interfaces;
using System.Diagnostics;
using OpenTelemetry.Trace;
using Pidp.Models.Lookups;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Requests to access defence counsel services will generate objects in the disclosure portal and not
/// the DEMS core/auf portals.
/// </summary>
public class DigitalEvidenceDefence
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public int FolioCaseId { get; set; }
        public string FolioId { get; set; }

        public string OrganizationType { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string ParticipantId { get; set; } = string.Empty;

    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {

            this.RuleFor(x => x.OrganizationName).NotEmpty();
            this.RuleFor(x => x.OrganizationType).NotEmpty();
            this.RuleFor(x => x.ParticipantId).NotEmpty();
            this.RuleFor(x => x.FolioId).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.FolioCaseId).GreaterThan(0);

        }
    }

    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly IKeycloakAdministrationClient keycloakClient;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IOrgUnitService orgUnitService;
        private readonly IKafkaProducer<string, EdtDisclosureUserProvisioning> kafkaProducer;
        private readonly IKafkaProducer<string, Notification> kafkaNotificationProducer;
        private readonly string SUBMITTING_AGENCY = "SubmittingAgency";
        private readonly string LAW_SOCIETY = "LawSociety";

        // EdtDisclosureUserProvisioning
        // we'll track the request in access requests and then push the request to a topic for disclosure service to handle
        public CommandHandler(
    IClock clock,
    IKeycloakAdministrationClient keycloakClient,
    ILogger<CommandHandler> logger,
    PidpConfiguration config,
    IOrgUnitService orgUnitService,
    PidpDbContext context,
    IKafkaProducer<string, EdtDisclosureUserProvisioning> kafkaProducer,
    IKafkaProducer<string, Notification> kafkaNotificationProducer)
        {
            this.clock = clock;
            this.keycloakClient = keycloakClient;
            this.logger = logger;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.config = config;
            this.kafkaNotificationProducer = kafkaNotificationProducer;
            this.orgUnitService = orgUnitService;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using IDbContextTransaction? trx = this.context.Database.BeginTransaction();

            using (var activity = new Activity("DigitalEvidenceDefence Request").Start())
            {


                try
                {

                    var traceId = Tracer.CurrentSpan.Context.TraceId;
                    Serilog.Log.Logger.Information("DigitalEvidenceDefence Request {0} {1} {2}", command.ParticipantId, command.FolioId, traceId);

                    Activity.Current?.AddTag("digitalevidence.party.id", command.PartyId);

                    var dto = await this.GetPidpUser(command);

                    if (dto.AlreadyEnroled
                        || dto.Email == null)
                    {
                        Serilog.Log.Logger.Warning($"DigitalEvidence Request denied for user {command.PartyId} Enrolled {dto.AlreadyEnroled}, Email {dto.Email}");
                        this.logger.LogDigitalEvidenceDisclosureAccessRequestDenied();
                        return DomainResult.Failed();
                    }

                    // create db entry
                    var disclosureUser = await this.SubmitDigitalEvidenceDisclosureRequest(command);

                    // publish message
                    var published = await this.PublishAccessRequest(command, dto, disclosureUser);



                    await this.context.SaveChangesAsync();
                    await trx.CommitAsync();

                    return DomainResult.Success();

                }
                catch (Exception ex)
                {
                    this.logger.LogDigitalEvidenceAccessTrxFailed(ex.Message.ToString());
                    return DomainResult.Failed();
                }


            }
        }

        private async Task<DeliveryResult<string, EdtDisclosureUserProvisioning>> PublishAccessRequest(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.ProducerTopicName, command.ParticipantId, taskId);
            var regions = new List<AssignedRegion>();

            var systemType = digitalEvidenceDisclosure.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal) ? AccessTypeCode.DigitalEvidenceDisclosure.ToString() : AccessTypeCode.DigitalEvidence.ToString();

            // use UUIDs for topic keys
            var delivered = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.ProducerTopicName, taskId, new EdtDisclosureUserProvisioning
            {
                Key = $"{command.ParticipantId}",
                UserName = dto.Jpdid,
                Email = dto.Email,
                PhoneNumber = dto.Phone!,
                FullName = $"{dto.FirstName} {dto.LastName}",
                AccountType = "Saml",
                Role = "User",
                SystemName = systemType,
                FolioCaseId = command.FolioCaseId,
                FolioId = command.FolioId,
                AccessRequestId = digitalEvidenceDisclosure.Id,
                OrganizationType = digitalEvidenceDisclosure.OrganizationType,
                OrganizationName = digitalEvidenceDisclosure.OrganizationName,
            });

            return delivered;
        }

        private async Task<Models.DigitalEvidenceDisclosure> SubmitDigitalEvidenceDisclosureRequest(Command command)
        {

            var digitalEvidenceDisclosure = new Models.DigitalEvidenceDisclosure
            {
                PartyId = command.PartyId,
                Status = AccessRequestStatus.Pending,
                OrganizationType = command.OrganizationType.ToString(),
                OrganizationName = command.OrganizationName,
                ParticipantId = command.ParticipantId,
                FolioCaseId = command.FolioCaseId,
                FolioId = command.FolioId,
                AccessTypeCode = AccessTypeCode.DigitalEvidence,
                RequestedOn = this.clock.GetCurrentInstant(),
            };
            this.context.DigitalEvidenceDisclosures.Add(digitalEvidenceDisclosure);

            await this.context.SaveChangesAsync();
            return digitalEvidenceDisclosure;
        }

        private async Task<PartyDto> GetPidpUser(Command command)
        {
            return await this.context.Parties
                .Where(party => party.Id == command.PartyId)
                .Select(party => new PartyDto
                {
                    AlreadyEnroled = party.AccessRequests.Any(request => request.AccessTypeCode == AccessTypeCode.DigitalEvidenceDisclosure),
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

public static partial class DigitalEvidenceDefenceLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Digital Evidence Access Request denied due to the Party Record not meeting all prerequisites.")]
    public static partial void LogDigitalEvidenceDisclosureAccessRequestDenied(this ILogger logger);
    [LoggerMessage(2, LogLevel.Warning, "Digital Evidence Access Request Transaction failed due to the Party Record not meeting all prerequisites.")]
    public static partial void LogDigitalEvidenceDisclosureAccessTrxFailed(this ILogger logger, string ex);

}
