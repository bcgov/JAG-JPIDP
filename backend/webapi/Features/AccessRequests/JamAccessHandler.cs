namespace Pidp.Features.AccessRequests;

using System.Threading.Tasks;
using Common.Exceptions;
using Common.Kafka;
using CommonModels.Models.JUSTIN;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Claims;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Models;
using Pidp.Models.Lookups;

public class JamAccessHandler
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public string TargetApp { get; set; } = "JAM_POR";
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {

            this.RuleFor(x => x.TargetApp).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);

        }
    }


    public class CommandHandler(
  IClock clock,
  IKeycloakAdministrationClient keycloakClient,
  IJUSTINClaimClient justinClaimClient,
  ILogger<JamAccessHandler.CommandHandler> logger,
  PidpConfiguration config,
  PidpDbContext context,
  IKafkaProducer<string, JAMProvisioningRequestModel> kafkaProducer) : ICommandHandler<Command, IDomainResult>
    {
        private readonly ILogger logger = logger;

        public async Task<IDomainResult> HandleAsync(Command command)
        {

            var dto = await this.GetPidpUser(command);
            try
            {
                if (dto.AlreadyEnroled
                    || dto.Email == null)
                {
                    Serilog.Log.Logger.Warning($"JAM Request denied for user {command.PartyId} Enrolled {dto.AlreadyEnroled}, Email {dto.Email}");
                    this.logger.LogDigitalEvidenceAccessRequestDenied();
                    return DomainResult.Failed();
                }
                var uid = Guid.NewGuid().ToString();

                // get the participant ID from JUSTIN for the UPN (email)

                var claims = await justinClaimClient.GetJustinClaims(dto.Email);

                if (claims == null || !string.IsNullOrEmpty(claims.Errors))
                {
                    if (claims != null)
                    {
                        return DomainResult.Failed($"Valid claims not found {claims.Errors}");
                    }
                    else
                    {
                        return DomainResult.Failed($"Failed to get claims for {dto.Email}");
                    }
                }

                var jamProjectUser = await this.SubmitJamApplicationAccessRequest(command, claims);





                var produceResponse = await kafkaProducer.ProduceAsync(config.KafkaCluster.JamUserProvisioningTopic, uid, new JAMProvisioningRequestModel
                {
                    KeycloakId = dto.UserId.ToString(),
                    PartyId = command.PartyId,
                    ParticipantId = claims.PartId,
                    UserId = claims.UserId,
                    UPN = dto.Email,
                    AccessRequestId = jamProjectUser.Id,
                    TargetApplication = command.TargetApp,

                });

                logger.LogInformation($"Access request for JAM sent to Kafka  {uid}, {command.PartyId} {dto.Jpdid}");

                return DomainResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError($"Access request failed {ex.Message}");

                return DomainResult.Failed(ex.Message);
            }

        }

        private async Task<JustinAppAccessRequest> SubmitJamApplicationAccessRequest(Command command, JUSTINClaimModel claims)
        {
            var jamRequest = new JustinAppAccessRequest
            {
                PartyId = command.PartyId,
                Status = AccessRequestStatus.Pending,
                ParticipantId = "" + claims.PartId,
                JustinUserId = claims.UserId,
                TargetApplication = command.TargetApp,
                AccessTypeCode = GetAccessType(command),
                RequestedOn = clock.GetCurrentInstant(),
            };
            context.JAMRequests.Add(jamRequest);

            await context.SaveChangesAsync();
            return jamRequest;
        }

        private static AccessTypeCode GetAccessType(Command command)
        {
            var targetApplication = command.TargetApp;

            AccessTypeCode accessType;
            if (command.TargetApp == "JAM_POR")
            {
                accessType = AccessTypeCode.JAMPOR;
            }
            else if (command.TargetApp == "JAM_RCC")
            {
                accessType = AccessTypeCode.JAMRCC;
            }
            else if (command.TargetApp == "JAM_LEA")
            {
                accessType = AccessTypeCode.JAMLEA;
            }
            else
            {
                throw new DIAMGeneralException("Invalid Target Application" + targetApplication);
            }

            return accessType;
        }


        private async Task<PartyDto> GetPidpUser(Command command)
        {




            return await context.Parties
                .Where(party => party.Id == command.PartyId)
                .Select(party => new PartyDto
                {
                    AlreadyEnroled = party.AccessRequests.Any(r => r.AccessTypeCode == GetAccessType(command)),
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
