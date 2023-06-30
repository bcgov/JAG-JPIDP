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
/// Requests to access defence counsel services will generate objects in the disclosure portal and the DEMS core portals.
/// </summary>
public class DigitalEvidenceDefence
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
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
            this.RuleFor(x => x.PartyId).GreaterThan(0);

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
        private readonly IKafkaProducer<string, EdtPersonProvisioningModel> kafkaDefenceCoreProducer;

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
        IKafkaProducer<string, EdtPersonProvisioningModel> kafkaDefenceCoreProducer,

    IKafkaProducer<string, Notification> kafkaNotificationProducer)
        {
            this.clock = clock;
            this.keycloakClient = keycloakClient;
            this.logger = logger;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.kafkaDefenceCoreProducer = kafkaDefenceCoreProducer;
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
                    Serilog.Log.Logger.Information($"DigitalEvidenceDefence Request {command.ParticipantId}  {command.PartyId} trace: [{traceId}]");

                    Activity.Current?.AddTag("digitalevidence.party.id", command.PartyId);

                    var dto = await this.GetPidpUser(command);

                    if (dto.AlreadyEnroled
                        || dto.Email == null)
                    {
                        Serilog.Log.Logger.Warning($"DigitalEvidence Request denied for user {command.PartyId} Enrolled {dto.AlreadyEnroled}, Email {dto.Email}");
                        this.logger.LogDigitalEvidenceDisclosureAccessRequestDenied();
                        return DomainResult.Failed();
                    }

                    // create db entry for disclosure access
                    var disclosureUser = await this.SubmitDigitalEvidenceDisclosureRequest(command);
                    // create entry for defence (core) access
                    var defenceUser = await this.SubmitDigitalEvidenceDefenceRequest(command);

                    // publish message for disclosure access
                    var publishedDisclosureRequest = await this.PublishDisclosureAccessRequest(command, dto, disclosureUser);

                    // publish message for disclosure access
                    var publishedDefenceRequest = await this.PublishDefenceAccessRequest(command, dto, defenceUser);

                    if (publishedDisclosureRequest.Status == PersistenceStatus.NotPersisted)
                    {
                        Serilog.Log.Error($"Failed to publish defence disclosure request for Defence Counsel user {defenceUser.Id}");
                        disclosureUser.Status = "Error";
                    }

                    if (publishedDefenceRequest.Status == PersistenceStatus.NotPersisted)
                    {
                        Serilog.Log.Error($"Failed to publish defence request for Defence Counsel user {defenceUser.Id}");
                        defenceUser.Status = "Error";
                    }



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

        private async Task<DeliveryResult<string, EdtPersonProvisioningModel>> PublishDefenceAccessRequest(Command command, PartyDto dto, Models.DigitalEvidenceDefence digitalEvidenceDefence)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.PersonCreationTopic, command.ParticipantId, taskId);

            var field = new EdtField
            {
                Name = "PPID",
                Value = command.ParticipantId
            };
       
            // use UUIDs for topic keys
            var delivered = await this.kafkaDefenceCoreProducer.ProduceAsync(this.config.KafkaCluster.PersonCreationTopic, taskId, new EdtPersonProvisioningModel
            {
                Key = $"{command.ParticipantId}",
                Address =
                {
                    Email = ( dto.Email != null ) ? dto.Email : "Not set"
                },
                Fields = new List<EdtField> { field },
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = "Participant",
                SystemName = AccessTypeCode.DigitalEvidenceDefence.ToString(),
                AccessRequestId = digitalEvidenceDefence.Id,
                OrganizationType = command.OrganizationType,
                OrganizationName = command.OrganizationName,
            });

            return delivered;
        }

        private async Task<DeliveryResult<string, EdtDisclosureUserProvisioning>> PublishDisclosureAccessRequest(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.DisclosureUserCreationTopic, command.ParticipantId, taskId);

            var systemType = digitalEvidenceDisclosure.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal) ? AccessTypeCode.DigitalEvidenceDisclosure.ToString() : AccessTypeCode.DigitalEvidence.ToString();


            // use UUIDs for topic keys
            var delivered = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.DisclosureUserCreationTopic, taskId, new EdtDisclosureUserProvisioning
            {
                Key = $"{command.ParticipantId}",
                UserName = dto.Jpdid,
                Email = dto.Email,
                PhoneNumber = dto.Phone!,
                FullName = $"{dto.FirstName} {dto.LastName}",
                AccountType = "Saml",
                Role = "User",
                SystemName = systemType,
                AccessRequestId = digitalEvidenceDisclosure.Id,
                OrganizationType = command.OrganizationType,
                OrganizationName = command.OrganizationName,
            });

            return delivered;
        }

        private async Task<Models.DigitalEvidenceDefence> SubmitDigitalEvidenceDefenceRequest(Command command)
        {

            var digitalEvidenceDefence = new Models.DigitalEvidenceDefence
            {
                PartyId = command.PartyId,
                Status = AccessRequestStatus.Pending,
                ParticipantId = command.ParticipantId,
                AccessTypeCode = AccessTypeCode.DigitalEvidenceDefence,
                RequestedOn = this.clock.GetCurrentInstant(),
            };
            this.context.DigitalEvidenceDefences.Add(digitalEvidenceDefence);

            await this.context.SaveChangesAsync();
            return digitalEvidenceDefence;
        }

        private async Task<DigitalEvidenceDisclosure> SubmitDigitalEvidenceDisclosureRequest(Command command)
        {

            var digitalEvidenceDisclosure = new Models.DigitalEvidenceDisclosure
            {
                PartyId = command.PartyId,
                Status = AccessRequestStatus.Pending,
                OrganizationType = command.OrganizationType.ToString(),
                OrganizationName = command.OrganizationName,
                ParticipantId = command.ParticipantId,
                AccessTypeCode = AccessTypeCode.DigitalEvidenceDisclosure,
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
