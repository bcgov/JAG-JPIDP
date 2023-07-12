namespace Pidp.Features.AccessRequests;

using System;
using System.Diagnostics;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        private readonly IKafkaProducer<string, UserChangeModel> kafkaAccountChangeProducer;

        private readonly IKafkaProducer<string, Notification> kafkaNotificationProducer;

        private static readonly Counter UserUpdateCounter = Metrics.CreateCounter("diam_user_updates", "Number of user updates");
        private static readonly Counter UserUpdateFailureCounter = Metrics.CreateCounter("diam_user_update_failure", "Number of user update failures");
        private static readonly Counter UserDeactivationCounter = Metrics.CreateCounter("diam_user_account_deactivation", "Number of user deactivations");

        public CommandHandler(
            IClock clock,
            IKeycloakAdministrationClient keycloakClient,
            IJumClient jumClient,
            ILogger<CommandHandler> logger,
            PidpConfiguration config,
            IOrgUnitService orgUnitService,
            PidpDbContext context,
            IKafkaProducer<string, EdtUserProvisioning> kafkaProducer,
            IKafkaProducer<string, UserChangeModel> kafkaAccountChangeProducer,
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
            this.kafkaAccountChangeProducer = kafkaAccountChangeProducer;
        }


        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using (var activity = new Activity("DigitalEvidence Account Change").Start())
            {

                UserUpdateCounter.Inc();
                this.logger.LogDigitalEvidenceAccessUpdateUserRequest(command.UserChangeEvent!.PartId);
                Activity.Current?.AddTag("digitalevidence.party.id", command.UserChangeEvent!.PartId);
                using var trx = this.context.Database.BeginTransaction();

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

                            var changes = await this.DetermineUserChanges(justinUserInfo.participantDetails.FirstOrDefault(), party, keycloakUserInfo);

                            if (!changes.ChangesDetected())
                            {
                                Serilog.Log.Information($"No changes were detected for user {party.Id}");
                                await trx.RollbackAsync();
                            }
                            else
                            { 
                                var settings = new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore,
                                    DefaultValueHandling = DefaultValueHandling.Ignore
                                };

                                // store the user change record
                                var changeEntry = this.context.UserAccountChanges.Add(new UserAccountChange
                                {
                                    Party = party,
                                    EventMessageId = command.UserChangeEvent.EventMessageId,
                                    ChangeData = JsonConvert.SerializeObject(changes, settings),
                                    Reason = "JUSTIN Change",
                                    TraceId = activity.TraceId.ToString(),
                                    Status = "Pending"

                                });

                                // if the change is for the email then we need to de-active the account and notify the user they need to on-board again
                                if (changes.SingleChangeTypes.ContainsKey(ChangeType.EMAIL) || changes.IsAccountDeactivated())
                                {

                                    // deactivate the account
                                    keycloakUserInfo!.Enabled = false;
                                    var deactivated = await this.UpdateKeycloakUser(party.UserId, keycloakUserInfo);

                                    if (deactivated)
                                    {
                                        UserDeactivationCounter.Inc();
                                        this.logger.LogKeycloakAccountDisabled(party.UserId.ToString());
                                    }
                                    else
                                    {
                                        this.logger.LogKeycloakAccountDisableFailure(party.UserId.ToString());
                                    }

                                    // notify user of deactiviation
                                    var eventType = changes.SingleChangeTypes.ContainsKey(ChangeType.EMAIL) ? "digitalevidence-bcps-userupdate-emailchanged" : "digitalevidence-bcps-userupdate-deactivated";
                                    var email = changes.SingleChangeTypes.ContainsKey(ChangeType.EMAIL) ? changes.SingleChangeTypes[ChangeType.EMAIL].To : party.Email;

                                    var eventData = new Dictionary<string, string>
                                {
                                    { "FirstName", party.FirstName! },
                                    { "AccessRequestId", "" + digitalEvidence.Id }
                                };

                                    var produceNotificationResponse = await this.kafkaNotificationProducer.ProduceAsync(this.config.KafkaCluster.NotificationTopicName, Guid.NewGuid().ToString(), new Notification
                                    {
                                        To = email,
                                        DomainEvent = eventType,
                                        EventData = eventData,
                                    });

                                    Serilog.Log.Information($"Change notification event for {party.Email} sent to {this.config.KafkaCluster.NotificationTopicName}");
                                }
                                else
                                {
                                    if (changes.SingleChangeTypes.ContainsKey(ChangeType.LASTNAME))
                                    {
                                        keycloakUserInfo.LastName = changes.SingleChangeTypes[ChangeType.LASTNAME].To;
                                    }

                                    if (changes.SingleChangeTypes.ContainsKey(ChangeType.FIRSTNAME))
                                    {
                                        keycloakUserInfo.FirstName = changes.SingleChangeTypes[ChangeType.FIRSTNAME].To;
                                    }

                                    if (changes.ListChangeTypes.ContainsKey(ChangeType.REGIONS))
                                    {
                                        Serilog.Log.Information($"Region changes for {party.UserId}");

                                        var regionChanges = changes.ListChangeTypes[ChangeType.REGIONS];
                                        var newRegions = regionChanges.To.Except(regionChanges.From).ToList();
                                        var removedRegions = regionChanges.From.Except(regionChanges.To).ToList();

                                        if (newRegions.Count > 0)
                                        {
                                            Serilog.Log.Information($"Adding [{string.Join(",", newRegions)}] regions for user {party.Id}");
                                            var removedGroupsOk = await this.AddKeycloakUserRegions(party.UserId, newRegions);
                                            if (regionChanges.From.Count() == 0)
                                            {
                                                // went from no groups to having groups - user is now active
                                                keycloakUserInfo.Enabled = true;
                                            }
                                        }

                                        if (removedRegions.Count > 0)
                                        {
                                            Serilog.Log.Information($"Removing [{string.Join(",", newRegions)}] regions for user {party.Id}");
                                            var removedGroupsOk = await this.RemoveKeycloakUserRegions(party.UserId, removedRegions);
                                        }


                                    }
                                }

                                var updated = await this.UpdateKeycloakUser(party.UserId, keycloakUserInfo);

                                await this.context.SaveChangesAsync();
                                await trx.CommitAsync();

                                var changeId = changeEntry.Entity.Id;
                                changes.ChangeId = changeId;


                                // flag the changes for other systems to process
                                var produceResponse = await this.kafkaAccountChangeProducer.ProduceAsync(this.config.KafkaCluster.UserAccountChangeTopicName, Guid.NewGuid().ToString(), changes);

                            }
                            
                        }


                    }
                    else
                    {
                        this.logger.LogNoDigitalEvidenceRequestFound(command.UserChangeEvent.PartId);
                        await trx.RollbackAsync();

                    }
                }
                catch (Exception ex)
                {
                    UserUpdateFailureCounter.Inc();
                    Serilog.Log.Error($"User update failed for {command.UserChangeEvent.PartId} with [{string.Join(",", ex.Message)}");
                    await trx.RollbackAsync();
                    return await DomainResult.FailedTask(string.Join(",", ex.Message));
                }

                return DomainResult.Success();
            }

        }

        private async Task<bool> AddKeycloakUserRegions(Guid userId, IEnumerable<string> groups)
        {


            foreach (var group in groups)
            {
                if (!await this.keycloakClient.AddGrouptoUser(userId, group))
                {
                    Serilog.Log.Logger.Error("Failed to add user {0} group {1} to keycloak", userId, group);
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> RemoveKeycloakUserRegions(Guid userId, IEnumerable<string> groups)
        {


            foreach (var group in groups)
            {
                if (!await this.keycloakClient.RemoveUserFromGroup(userId, group))
                {
                    Serilog.Log.Logger.Error("Failed to remove user {0} from keycloak group {1} ", userId, group);
                    return false;
                }
            }

            return true;
        }


        private async Task<bool> UpdateKeycloakUser(Guid userId, UserRepresentation user)
        {
            Serilog.Log.Information($"Keycloak account update for {user.Email}");

            return await this.keycloakClient.UpdateUser(userId, user);
        }

        /// <summary>
        /// Determine what has been changed for a user
        /// </summary>
        /// <param name="justinUserInfo"></param>
        /// <param name="party"></param>
        /// <param name="keycloakUserInfo"></param>
        /// <returns></returns>
        private async Task<UserChangeModel> DetermineUserChanges(ParticipantDetail justinUserInfo, Party party, UserRepresentation? keycloakUserInfo)
        {

            UserChangeModel userChangeModel = new UserChangeModel();
            // the user account for these users is the email address from JUSTIN
            userChangeModel.UserID = party.Email;
            userChangeModel.Key = justinUserInfo.partId;

            // see if email has changed - case insensitive
            if (!string.IsNullOrEmpty(justinUserInfo.emailAddress))
            {
                if (!party.Email.Equals(justinUserInfo.emailAddress, StringComparison.OrdinalIgnoreCase))
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
            if (!justinUserInfo.firstGivenNm.Equals(party.FirstName, StringComparison.Ordinal))
            {
                Serilog.Log.Information($"User {party.Id} first name changed from {party.FirstName} to {justinUserInfo.firstGivenNm}");
                userChangeModel.SingleChangeTypes.Add(ChangeType.FIRSTNAME, new SingleChangeType(party.FirstName, justinUserInfo.firstGivenNm));
            }


            // see if roles have changed TODO - do we care about these??
            if (justinUserInfo.GrantedRoles.Count > 0)
            {
                var justinRoles = justinUserInfo.GrantedRoles.Select(role => role.role).ToList();
            }
            else
            {
                Serilog.Log.Information($"User {party.Id} has no granted roles in JUSTIN - disabling account");
                userChangeModel.BooleanChangeTypes.Add(ChangeType.ACTIVATION, new BooleanChangeType(true, false));
            }

            // see if regions has changed
            if (justinUserInfo.assignedAgencies.Count > 0)
            {
                var justinAgencies = justinUserInfo.assignedAgencies.Select(agency => agency.agencyName).ToList();

                var groups = await this.keycloakClient.GetUserGroups(party.UserId);

                // check which groups the user is in now
                var keycloakGroups = groups.Select(group => group.Name).ToList();

                // user should be in BCPS group
                if (!keycloakGroups.Contains("BCPS"))
                {
                    Serilog.Log.Warning($"User {party.Id} is not currently in the BCPS group");
                }

                // get all regions (e.g. Van Isl, Interior) from the list of assigned regions in JUSTIN
                var assignedJUSTINRegions = await this.GetCrownRegions(justinAgencies);
                // get all known regions
                var allRegions = await this.GetAllCrownRegions();

                // get the regions assigned in keycloak that are valid regions (could be additional groups added that are unrelated)
                var keycloakRegions = allRegions.Intersect(keycloakGroups).ToList();

                // find the differences
                var unwantedRegions = keycloakRegions.Except(assignedJUSTINRegions).ToList();
                var newRegions = assignedJUSTINRegions.Except(keycloakRegions).ToList();

                if (unwantedRegions.Count > 0 || newRegions.Count > 0)
                {
                    Serilog.Log.Information($"{party.Id} has group assignment (region) changes from [{string.Join(",", keycloakRegions)}] to [{string.Join(",", assignedJUSTINRegions)}]");
                    userChangeModel.ListChangeTypes.Add(ChangeType.REGIONS, new ListChangeType(keycloakRegions, assignedJUSTINRegions));

                }


            }
            else
            {

                Serilog.Log.Information($"User {party.Id} has no granted agencies in JUSTIN - disabling account");
                userChangeModel.BooleanChangeTypes.Add(ChangeType.ACTIVATION, new BooleanChangeType(true, false));
            }

            return userChangeModel;
        }

        private async Task<List<string>> GetCrownRegions(List<string> AssignedAgencies)
        {
            var query = new Lookups.Index.Query();
            var handler = new Lookups.Index.QueryHandler(this.context);

            List<CrownRegion>? regions = handler.HandleAsync(query).Result.CrownRegions;

            var assignedCrownRegions = regions.Where(region => AssignedAgencies.Any(x => region.CrownLocation.Equals(x, StringComparison.OrdinalIgnoreCase)));

            // get the unique list of regions
            var assignedRegions = assignedCrownRegions.Select(region => region.RegionName).Distinct().ToList();

            return assignedRegions;

        }

        private async Task<List<string>> GetAllCrownRegions()
        {
            var query = new Lookups.Index.Query();
            var handler = new Lookups.Index.QueryHandler(this.context);

            List<CrownRegion>? regions = handler.HandleAsync(query).Result.CrownRegions;
            var distinctRegions = regions.Select(region => region.RegionName).Distinct().ToList();

            return distinctRegions;

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
    [LoggerMessage(4, LogLevel.Information, "Keycloak account disabled for {userId}")]
    public static partial void LogKeycloakAccountDisabled(this ILogger logger, string userId);
    [LoggerMessage(5, LogLevel.Error, "Failed to disable keycloak account for {userId}")]
    public static partial void LogKeycloakAccountDisableFailure(this ILogger logger, string userId);
}

