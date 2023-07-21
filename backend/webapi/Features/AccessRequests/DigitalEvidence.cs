namespace Pidp.Features.AccessRequests;

using System.Diagnostics;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;

using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;
using OpenTelemetry.Trace;
using Newtonsoft.Json;
using Pidp.Features.Parties;
using Pidp.Features.Organization.OrgUnitService;
using Confluent.Kafka;

public class DigitalEvidence
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public string OrganizationType { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string ParticipantId { get; set; } = string.Empty;
        public List<AssignedRegion> AssignedRegions { get; set; } = new List<AssignedRegion>();
    }
    public enum UserType
    {
        BCPS = 1,
        OutOfCustody,
        Police,
        Lawyer,
        None
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.OrganizationName).NotEmpty();
            this.RuleFor(x => x.OrganizationType).NotEmpty();
            this.RuleFor(x => x.ParticipantId).NotEmpty();
            this.RuleFor(x => x.AssignedRegions).ForEach(x => x.NotEmpty());
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
        private readonly IKafkaProducer<string, EdtUserProvisioning> kafkaProducer;
        private readonly IKafkaProducer<string, Notification> kafkaNotificationProducer;
        private readonly string SUBMITTING_AGENCY = "SubmittingAgency";
        private readonly string LAW_SOCIETY = "LawSociety";


        public CommandHandler(
            IClock clock,
            IKeycloakAdministrationClient keycloakClient,
            ILogger<CommandHandler> logger,
            PidpConfiguration config,
            IOrgUnitService orgUnitService,
            PidpDbContext context,
            IKafkaProducer<string, EdtUserProvisioning> kafkaProducer,
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

            using (var activity = new Activity("DigitalEvidence Request").Start())
            {

                var traceId = Tracer.CurrentSpan.Context.TraceId;
                Serilog.Log.Logger.Information("DigitalEvidence Request {0} {1}", command.ParticipantId, traceId);

                Activity.Current?.AddTag("digitalevidence.party.id", command.PartyId);


                var dto = await this.GetPidpUser(command);

                if (dto.AlreadyEnroled
                    || dto.Email == null)
                {
                    Serilog.Log.Logger.Warning($"DigitalEvidence Request denied for user {command.PartyId} Enrolled {dto.AlreadyEnroled}, Email {dto.Email}");
                    this.logger.LogDigitalEvidenceAccessRequestDenied();
                    return DomainResult.Failed();
                }

                if (!await this.UpdateKeycloakUser(dto.UserId, command.AssignedRegions, command.ParticipantId))
                {
                    return DomainResult.Failed();
                }

                using var trx = this.context.Database.BeginTransaction();
                try
                {

                    var digitalEvidence = await this.SubmitDigitalEvidenceRequest(command); //save all trx at once for production(remove this and handle using idempotent)
                    var key = Guid.NewGuid().ToString();

                   // var exportedEvent = this.AddOutbox(command, digitalEvidence, dto);

                    var published = await this.PublishAccessRequest(command, dto, digitalEvidence);

                    if ( published.Status == PersistenceStatus.Persisted)
                    {
                        await this.context.SaveChangesAsync();
                        await trx.CommitAsync();
                    }
                    else
                    {
                        this.logger.LogDigitalEvidenceAccessTrxFailed($"Failed to publish access request to topic {digitalEvidence} - rolling back transaction");

                        await trx.RollbackAsync();
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

        private async Task<DeliveryResult<string, EdtUserProvisioning>> PublishAccessRequest(Command command, PartyDto dto, Models.DigitalEvidence digitalEvidence)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.ProducerTopicName, command.ParticipantId, taskId);
            var regions = new List<AssignedRegion>();

            if (!digitalEvidence.OrganizationType.Equals(this.SUBMITTING_AGENCY, StringComparison.Ordinal) && !digitalEvidence.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal))
            {
                // get the assigned regions again - this prevents sending requests with an altered list of regions
                var query = new CrownRegionQuery.Query(command.PartyId, Convert.ToDecimal(command.ParticipantId));

                // create an instance of the QueryHandler class
                var handler = new CrownRegionQuery.QueryHandler(this.orgUnitService);

                IEnumerable<OrgUnitModel?>? orgUnits = await handler.HandleAsync(query);
                regions = this.ConvertOrgUnitRegions(orgUnits);

                // execute the query and get the result
                var result = handler.HandleAsync(query);
            }

            var systemType = digitalEvidence.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal) ? AccessTypeCode.DigitalEvidenceDisclosure.ToString() : AccessTypeCode.DigitalEvidence.ToString();

            // use UUIDs for topic keys
            var delivered = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.ProducerTopicName, taskId, new EdtUserProvisioning
            {
                Key = $"{command.ParticipantId}",
                UserName = dto.Jpdid,
                Email = dto.Email,
                PhoneNumber = dto.Phone!,
                FullName = $"{dto.FirstName} {dto.LastName}",
                AccountType = "Saml",
                Role = "User",
                SystemName = systemType,
                AssignedRegions = regions,
                AccessRequestId = digitalEvidence.Id,
                OrganizationType = digitalEvidence.OrganizationType,
                OrganizationName = digitalEvidence.OrganizationName,
            });

            return delivered;
        }

        private List<AssignedRegion?>? ConvertOrgUnitRegions(IEnumerable<OrgUnitModel?> orgUnits)
        {
            var regions = new List<AssignedRegion>();
            foreach (var orgUnit in orgUnits)
            {

                regions.Add(new AssignedRegion
                {
                    AssignedAgency = orgUnit.AssignedAgency,
                    RegionId = orgUnit.RegionId,
                    RegionName = orgUnit.RegionName
                });

            }
            return regions;
        }

        private async Task<Models.DigitalEvidence> SubmitDigitalEvidenceRequest(Command command)
        {

            var digitalEvidence = new Models.DigitalEvidence
            {
                PartyId = command.PartyId,
                Status = AccessRequestStatus.Pending,
                OrganizationType = command.OrganizationType.ToString(),
                OrganizationName = command.OrganizationName,
                ParticipantId = command.ParticipantId,
                AccessTypeCode = AccessTypeCode.DigitalEvidence,
                RequestedOn = this.clock.GetCurrentInstant(),
                AssignedRegions = command.AssignedRegions
            };
            this.context.DigitalEvidences.Add(digitalEvidence);

            await this.context.SaveChangesAsync();
            return digitalEvidence;
        }
        private Task<Models.OutBoxEvent.ExportedEvent> AddOutbox(Command command, Models.DigitalEvidence digitalEvidence, PartyDto dto)
        {
            var exportedEvent = this.context.ExportedEvents.Add(new Models.OutBoxEvent.ExportedEvent
            {
                AggregateType = AccessTypeCode.DigitalEvidence.ToString(),
                AggregateId = $"{command.PartyId}",
                EventType = "Access Request Created",
                EventPayload = JsonConvert.SerializeObject(new EdtUserProvisioning
                {
                    Key = $"{command.ParticipantId}",
                    UserName = dto.Jpdid,
                    Email = dto.Email,
                    PhoneNumber = dto.Phone!,
                    FullName = $"{dto.FirstName} {dto.LastName}",
                    AccountType = "Saml",
                    Role = "User",
                    AssignedRegions = command.AssignedRegions
                })
            });
            return Task.FromResult(exportedEvent.Entity);
        }

        private async Task<bool> UpdateKeycloakUser(Guid userId, IEnumerable<AssignedRegion> assignedGroup, string partId)
        {
            if (!await this.keycloakClient.UpdateUser(userId, (user) => user.SetPartId(partId)))
            {
                Serilog.Log.Logger.Error("Failed to set user {0} partId in keycloak", partId);

                return false;
            }
            foreach (var group in assignedGroup)
            {
                if (!await this.keycloakClient.AddGrouptoUser(userId, group.RegionName))
                {
                    Serilog.Log.Logger.Error("Failed to add user {0} group {1} to keycloak", partId, group.RegionName);
                    return false;
                }
            }

            return true;
        }
    }
}



public static partial class DigitalEvidenceLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Digital Evidence Access Request denied due to the Party Record not meeting all prerequisites.")]
    public static partial void LogDigitalEvidenceAccessRequestDenied(this ILogger logger);
    [LoggerMessage(2, LogLevel.Warning, "Digital Evidence Access Request Transaction failed due to the error {error}.")]
    public static partial void LogDigitalEvidenceAccessTrxFailed(this ILogger logger, string error);

}
