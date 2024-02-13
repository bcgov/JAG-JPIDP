namespace Pidp.Features.AccessRequests;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Common.Models.Approval;
using Common.Models.Notification;
using Confluent.Kafka;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;
using Prometheus;



/// <summary>
/// Requests to access disclosure portal for accused (public BCSC users)
/// </summary>
public class DigitalEvidencePublicDisclosure
{

    private static readonly Counter PublicDisclosureUserCounter = Metrics.CreateCounter("diam_disclosure_public_user_total", "Number of public disclosure users");
    private static readonly Histogram PublicDisclosureUserDuration = Metrics.CreateHistogram("diam_disclosure_public_histogram", "Duration of public disclosure user access requests");


    public class Command : ICommand<IDomainResult>
    {
        [Required]
        public int PartyId { get; set; }
        [Required]
        public string ParticipantId { get; set; } = string.Empty;
        [Required]
        public string KeyData { get; set; } = string.Empty;


    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {

            this.RuleFor(x => x.ParticipantId).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.KeyData).NotEmpty();

        }
    }


    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly IKeycloakAdministrationClient keycloakClient;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly IEdtCoreClient coreClient;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, EdtDisclosureUserProvisioning> kafkaProducer;
        private readonly IKafkaProducer<string, ApprovalRequestModel> approvalKafkaProducer;
        private readonly IKafkaProducer<string, Notification> kafkaNotificationProducer;


        // EdtDisclosureUserProvisioning
        // we'll track the request in access requests and then push the request to a topic for disclosure service to handle
        public CommandHandler(
            IClock clock,
            IKeycloakAdministrationClient keycloakClient,
            ILogger<CommandHandler> logger,
            PidpConfiguration config,
            PidpDbContext context,
            IEdtCoreClient coreClient,
            IKafkaProducer<string, EdtDisclosureUserProvisioning> kafkaProducer,
            IKafkaProducer<string, ApprovalRequestModel> approvalKafkaProducer,
            IKafkaProducer<string, Notification> kafkaNotificationProducer)
        {
            this.clock = clock;
            this.keycloakClient = keycloakClient;
            this.logger = logger;
            this.coreClient = coreClient;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.approvalKafkaProducer = approvalKafkaProducer;
            this.config = config;
            this.kafkaNotificationProducer = kafkaNotificationProducer;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using (PublicDisclosureUserDuration.NewTimer())
            {
                Serilog.Log.Information($"Public disclosure access request {command.ParticipantId} {command.PartyId} {command.KeyData}");

                var trx = this.context.Database.BeginTransaction();

                using (var activity = new Activity("DigitalEvidenceDisclosure Request").Start())
                {
                    // check for prior request
                    var dto = await this.GetPidpUser(command);

                    if (dto.AlreadyEnroled)
                    {
                        Serilog.Log.Logger.Information($"User {command.PartyId} is already enrolled [{dto.UserId}]- can be request for additional folio access  Key {command.KeyData}");
                    }

                    // store the access request - public users may request multiple times due to participant merges and multiple unique resulting IDs
                    var publicRequest = new Models.DigitalEvidencePublicDisclosure
                    {
                        RequestedOn = this.clock.GetCurrentInstant(),
                        PartyId = command.PartyId,
                        Status = AccessRequestStatus.Pending,
                        AccessTypeCode = AccessTypeCode.DigitalEvidenceDisclosure,
                        Created = this.clock.GetCurrentInstant(),
                        KeyData = command.KeyData
                    };

                    this.context.DigitalEvidencePublicDisclosures.Add(publicRequest);

                    // submit a request to the appropriate topic
                    await this.context.SaveChangesAsync();
                    // place a request on the topic
                    var response = await this.PublishDisclosureAccessRequest(command, dto, publicRequest);

                    if (response.Status == PersistenceStatus.NotPersisted)
                    {
                        Serilog.Log.Error($"Failed to publish public disclosure request for out of custody user {dto}");
                        publicRequest.Status = AccessRequestStatus.Failed;
                        await trx.RollbackAsync();

                        return DomainResult.Failed($"Failed to publish public disclosure request for out of custody user {dto}");
                    }



                    await trx.CommitAsync();


                    return DomainResult.Success();
                }

            }
        }




        private async Task<DeliveryResult<string, EdtDisclosureUserProvisioning>> PublishDisclosureAccessRequest(Command command, PartyDto dto, Models.DigitalEvidencePublicDisclosure digitalEvidencePublicDisclosure)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.DisclosurePublicUserCreationTopic, command.ParticipantId, taskId);

            // use UUIDs for topic keys
            var delivered = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.DisclosurePublicUserCreationTopic,
                taskId,
                this.GetPublicDisclosureUserModel(command, dto, digitalEvidencePublicDisclosure));

            return delivered;
        }

        private EdtDisclosureUserProvisioning GetPublicDisclosureUserModel(Command command, PartyDto dto, Models.DigitalEvidencePublicDisclosure digitalEvidencePublicDisclosure)
        {

            return new EdtDisclosureUserProvisioning
            {
                Key = $"{command.ParticipantId}",
                UserName = dto.Jpdid,
                Email = dto.Email,
                FullName = $"{dto.FirstName} {dto.LastName}",
                AccountType = "Saml",
                Role = "User",
                SystemName = AccessTypeCode.DigitalEvidenceDisclosure.ToString(),
                AccessRequestId = digitalEvidencePublicDisclosure.Id,
                OrganizationType = "Out-of-custody",
                OrganizationName = "Public",
                PersonKey = command.KeyData
            };
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
