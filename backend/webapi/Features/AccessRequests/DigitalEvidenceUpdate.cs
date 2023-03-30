namespace Pidp.Features.AccessRequests;

using System;
using System.Diagnostics.Metrics;
using System.Text.Json.Nodes;
using DomainResults.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Infrastructure.HttpClients.Jum;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Consumer.JustinUserChanges;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;
using Prometheus;

/// <summary>
/// This command is responsible for updating users - will update the user in keycloak and send a request to EDT to also
/// update the user
/// </summary>
public class DigitalEvidenceUpdate
{
    public class Command : ICommand<IDomainResult>
    {
        public JustinUserChangeEvent? UserChangeEvent { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(command => command.UserChangeEvent).NotNull();
            this.RuleFor(command => command.UserChangeEvent.PartId).NotEmpty();
            // we currently only accept JUSTIN sourced changes
            this.RuleFor(command => command.UserChangeEvent.Source).Equal("JUSTIN");
            this.RuleFor(command => command.UserChangeEvent.EventMessageId).GreaterThan(0);
        }
    }




    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly IKeycloakAdministrationClient keycloakClient;
        private readonly ILogger logger;
        private readonly IJumClient jumClient;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IOrgUnitService orgUnitService;
        private readonly IKafkaProducer<string, EdtUserProvisioning> kafkaProducer;
        private readonly IKafkaProducer<string, Notification> kafkaNotificationProducer;
        private static readonly Counter UserUpdateCounter = Metrics.CreateCounter("dems_user_updates", "Number of user updates");
        private static readonly Counter UserUpdateFailureCounter = Metrics.CreateCounter("dems_user_update_failure", "Number of user update failures");

        public CommandHandler(
            IClock clock,
            IKeycloakAdministrationClient keycloakClient,
            IJumClient jumClient,
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
            this.jumClient = jumClient;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.config = config;
            this.kafkaNotificationProducer = kafkaNotificationProducer;
            this.orgUnitService = orgUnitService;
        }


        public async Task<IDomainResult> HandleAsync(Command command)
        {
            UserUpdateCounter.Inc();
            this.logger.LogDigitalEvidenceAccessUpdateUserRequest(command.UserChangeEvent!.PartId);

            try
            {
                // get the user from the original request
                var digitalEvidence = this.context.DigitalEvidences.Include(x => x.Party).Where(x => x.ParticipantId == "" + command.UserChangeEvent.PartId).FirstOrDefault();
        
                if (digitalEvidence != null)
                {
                    // get the party record
                    var party = digitalEvidence.Party;
                    Serilog.Log.Information($"Update for party {party.Email}");

                    var justinUserInfo = await this.jumClient.GetJumUserByPartIdAsync(command.UserChangeEvent.PartId);

                    if (justinUserInfo == null)
                    {
                        this.logger.LogNoRecordFound(command.UserChangeEvent.PartId);
                    }
                    else
                    {
                        // determine what has changed
                        var keycloakUserInfo = await this.keycloakClient.GetUser(party.UserId);

                        Serilog.Log.Information($"Keycloak user {keycloakUserInfo}");

                        var changes = this.DetermineUserChanges(justinUserInfo.participantDetails.FirstOrDefault(), party, keycloakUserInfo);


                    }
                }
                else
                {
                    this.logger.LogNoDigitalEvidenceRequestFound(command.UserChangeEvent.PartId);
                }
            }
            catch (Exception ex)
            {
                UserUpdateFailureCounter.Inc();
                Serilog.Log.Error($"User update failed for {command.UserChangeEvent.PartId} with [{string.Join(",", ex.Message)}");
                return await DomainResult.FailedTask(string.Join(",", ex.Message));
            }

            return DomainResult.Success();

        }

        private UserChangeModel DetermineUserChanges(ParticipantDetail justinUserInfo, Party party, UserRepresentation? keycloakUserInfo)
        {

            UserChangeModel userChangeModel = new UserChangeModel();
            // the user account for these users is the email address from JUSTIN
            userChangeModel.UserID = party.Email;

            // see if email has changed
            if (!string.IsNullOrEmpty(justinUserInfo.emailAddress))
            {
                if (!party.Email.Equals(justinUserInfo.emailAddress, StringComparison.Ordinal))
                {
                    Serilog.Log.Information($"User {party.Id} email changed from {party.Email} to {justinUserInfo.emailAddress}");
                    userChangeModel.SingleChangeTypes.Add(ChangeType.EMAIL, new SingleChangeType(party.Email, justinUserInfo.emailAddress));
                }
            }

            // see if name has changed
            if (!justinUserInfo.surname.Equals(party.LastName, StringComparison.Ordinal))
            {
                Serilog.Log.Information($"User {party.Id} last name changed from {party.LastName} to {justinUserInfo.surname}");
                userChangeModel.SingleChangeTypes.Add(ChangeType.LASTNAME, new SingleChangeType(party.LastName, justinUserInfo.surname));
            }
            if (!justinUserInfo.surname.Equals(party.LastName, StringComparison.Ordinal))
            {
                Serilog.Log.Information($"User {party.Id} first name changed from {party.FirstName} to {justinUserInfo.firstGivenNm}");
                userChangeModel.SingleChangeTypes.Add(ChangeType.FIRSTNAME, new SingleChangeType(party.FirstName, justinUserInfo.firstGivenNm));
            }

            // see if roles have changed
            if (justinUserInfo.GrantedRoles.Count > 0)
            {
                List<string> justinRoles = justinUserInfo.GrantedRoles.Select(role => role.role).ToList();

            }
            else
            {
                Serilog.Log.Information($"User {party.Id} has no granted roles in JUSTIN - disabling account");
                userChangeModel.BooleanChangeTypes.Add(ChangeType.ACTIVATION, new BooleanChangeType(true, false));
            }

            // see if regions has changed
            if (justinUserInfo.assignedAgencies.Count > 0)
            {
                List<string> justinAgencies = justinUserInfo.assignedAgencies.Select( agency => agency.agencyName ).ToList();
                

            }
            else
            {
                Serilog.Log.Information($"User {party.Id} has no granted agencies in JUSTIN - disabling account");
                userChangeModel.BooleanChangeTypes.Add(ChangeType.ACTIVATION, new BooleanChangeType(true, false));
            }

            return userChangeModel;
        }

    }
}

public static partial class DigitalEvidenceUpdateLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Digital Evidence User Update received for {partId}")]
    public static partial void LogDigitalEvidenceAccessUpdateUserRequest(this ILogger logger, decimal partId);
    [LoggerMessage(2, LogLevel.Error, "No record found for user with partid {partId}")]
    public static partial void LogNoRecordFound(this ILogger logger, decimal partId);
    [LoggerMessage(3, LogLevel.Warning, "No digital eveidence request found for partId {partId}")]
    public static partial void LogNoDigitalEvidenceRequestFound(this ILogger logger, decimal partId);
}

