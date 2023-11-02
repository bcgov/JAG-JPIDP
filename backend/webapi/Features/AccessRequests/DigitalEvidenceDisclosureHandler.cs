namespace Pidp.Features.AccessRequests;

using System.Diagnostics;
using Common.Models.Approval;
using Common.Models.Notification;
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
public class DigitalEvidenceDisclosureHandler
{

    private static readonly Counter PublicDisclosureUserCounter = Metrics.CreateCounter("diam_disclosure_public_user_total", "Number of public disclosure users");
    private static readonly Histogram PublicDisclosureUserDuration = Metrics.CreateHistogram("diam_disclosure_public_histogram", "Duration of public disclosure user access requests");


    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public string ParticipantId { get; set; } = string.Empty;
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


                    // submit a request to the appropriate topic


                    return DomainResult.Success();
                }

            }
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
