namespace Pidp.Features.AccessRequests;

using System.Diagnostics;
using System.Linq;
using common.Constants.Auth;
using Common.Models.Approval;
using Common.Models.Notification;
using Confluent.Kafka;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodaTime;
using OpenTelemetry.Trace;
using Pidp.Data;
using Pidp.Exceptions;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;

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
        private readonly IKafkaProducer<string, ApprovalRequestModel> approvalKafkaProducer;

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
    IKafkaProducer<string, ApprovalRequestModel> approvalKafkaProducer,
        IKafkaProducer<string, EdtPersonProvisioningModel> kafkaDefenceCoreProducer,
    IKafkaProducer<string, Notification> kafkaNotificationProducer)
        {
            this.clock = clock;
            this.keycloakClient = keycloakClient;
            this.logger = logger;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.approvalKafkaProducer = approvalKafkaProducer;
            this.kafkaDefenceCoreProducer = kafkaDefenceCoreProducer;
            this.config = config;
            this.kafkaNotificationProducer = kafkaNotificationProducer;
            this.orgUnitService = orgUnitService;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using var trx = this.context.Database.BeginTransaction();

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
                        await trx.RollbackAsync();

                        return DomainResult.Failed();
                    }

                    // get the user details from keycloak and check they are valid - otherwise will require an approval step
                    var keycloakUser = await this.keycloakClient.GetUser(dto.UserId);

                    if (keycloakUser == null)
                    {
                        await trx.RollbackAsync();

                        throw new AccessRequestException($"No keycloak user found with id {dto.UserId}");
                    }

                    var userValidationErrors = IsKeycloakUserValid(keycloakUser);

                    // create db entry for disclosure access
                    var disclosureUser = await this.SubmitDigitalEvidenceDisclosureRequest(command);
                    // create entry for defence (core) access
                    var defenceUser = await this.SubmitDigitalEvidenceDefenceRequest(command);

                    var permitMismatchedVCCreds = Environment.GetEnvironmentVariable("PERMIT_MISMATCH_VC_CREDS");
                    var ignoreBCServiceCard = false;
                    if (permitMismatchedVCCreds != null && permitMismatchedVCCreds.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        ignoreBCServiceCard = true;
                    }

                    if (userValidationErrors.Count > 0 && !ignoreBCServiceCard)
                    {
                        Serilog.Log.Warning($"User {keycloakUser} has errors {string.Join(",", userValidationErrors)}");
                        Serilog.Log.Information("User request will need to go through approval flows");
                        disclosureUser.Status = AccessRequestStatus.RequiresApproval;
                        defenceUser.Status = AccessRequestStatus.RequiresApproval;

                        // create approval message
                        var deliveryResult = await this.PublishApprovalRequest(keycloakUser, command, dto, userValidationErrors, new List<Models.AccessRequest> { defenceUser, disclosureUser });

                        foreach (var result in deliveryResult)
                        {
                            if (result.Status == PersistenceStatus.Persisted)
                            {
                                Serilog.Log.Information($"Published {result.Key} to {result.Partition}");

                                // store the original requests for later
                                if (await this.CreateDefenceDeferredPublishRequest(command, dto, defenceUser) && await this.CreateDisclosureDeferredPublishRequest(command, dto, disclosureUser))
                                {
                                    Serilog.Log.Information($"Stored requests for later use");
                                }
                                else
                                {
                                    Serilog.Log.Error($"Failed to store requests for later - we will need to reset this request");
                                }
                            }
                            else
                            {
                                Serilog.Log.Error($"Failed to publish {result.Key} to {result.Status}");

                            }


                        }
                    }
                    else
                    {

                        if (ignoreBCServiceCard && userValidationErrors.Count > 0)
                        {
                            Serilog.Log.Warning($"User {keycloakUser} has errors {string.Join(",", userValidationErrors)} - but we are going to ignore these due to env flag PERMIT_MISMATCH_VC_CREDS");
                        }

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

        /// <summary>
        /// Check if the names match on the user credentials and the user is still a practicing member
        /// </summary>
        /// <param name="keycloakUser"></param>
        /// <returns></returns>
        private static List<string> IsKeycloakUserValid(UserRepresentation keycloakUser)
        {
            var hasFamilyName = keycloakUser.Attributes.TryGetValue(Claims.BcPersonFamilyName, out var BCFamilyName);
            var hasFirstName = keycloakUser.Attributes.TryGetValue(Claims.BcPersonGivenName, out var BCFirstName);
            var hasMemberStatus = keycloakUser.Attributes.TryGetValue(Claims.MembershipStatusCode, out var memberShipStatus);
            var errors = new List<string>();

            if (!hasFamilyName || !hasFirstName)
            {
                Serilog.Log.Error($"No BC First or Family name found in claims {keycloakUser}");
                errors.Add("No BC First or Family name found in claims");
            }

            if (!hasMemberStatus)
            {
                Serilog.Log.Error($"No member status found for user {keycloakUser}");
                errors.Add("No member status found for user");


            }

            if (!string.Join(" ", BCFamilyName).Equals(keycloakUser.LastName, StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Error($"User family name does not match between BCSC [{string.Join(" ", BCFamilyName)}] and BCLaw [{keycloakUser.LastName}] {keycloakUser}");
                errors.Add($"User family name does not match between BCSC [{string.Join(" ", BCFamilyName)}] and BCLaw [{keycloakUser.LastName}]");

            }


            if (!string.Join(" ", BCFirstName).Equals(keycloakUser.FirstName, StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Error($"User first name does not match between BCSC [{string.Join(" ", BCFirstName)}] and BCLaw [{keycloakUser.FirstName}] {keycloakUser}");
                errors.Add($"User first name does not match between BCSC [{string.Join(" ", BCFirstName)}] and BCLaw [{keycloakUser.FirstName}]");

            }

            // not practicing - shouldnt get this far but checking just in case!
            if (memberShipStatus != null && !string.Join("", memberShipStatus).Equals("PRAC", StringComparison.Ordinal))
            {
                Serilog.Log.Error($"User is not shown as currently practicing [{string.Join("", memberShipStatus)}] {keycloakUser}");
                errors.Add($"User is not shown as currently practicing");
            }

            return errors;
        }

        private async Task<DeliveryResult<string, EdtPersonProvisioningModel>> PublishDefenceAccessRequest(Command command, PartyDto dto, Models.DigitalEvidenceDefence digitalEvidenceDefence)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.PersonCreationTopic, command.ParticipantId, taskId);

            // use UUIDs for topic keys
            var delivered = await this.kafkaDefenceCoreProducer.ProduceAsync(this.config.KafkaCluster.PersonCreationTopic, taskId, this.GetEdtPersonModel(command, dto, digitalEvidenceDefence));

            return delivered;
        }

        private EdtDisclosureUserProvisioning GetDisclosureUserModel(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
        {
            var systemType = digitalEvidenceDisclosure.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal) ? AccessTypeCode.DigitalEvidenceDisclosure.ToString() : AccessTypeCode.DigitalEvidence.ToString();


            return new EdtDisclosureUserProvisioning
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
            };
        }

        private EdtPersonProvisioningModel GetEdtPersonModel(Command command, PartyDto dto, Models.DigitalEvidenceDefence digitalEvidenceDefence)
        {


            return new EdtPersonProvisioningModel
            {
                Key = $"{command.ParticipantId}",
                Address =
                {
                    Email = dto.Email ?? "Not set"
                },
                Fields = new List<EdtField> { },
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = "Participant",
                SystemName = AccessTypeCode.DigitalEvidenceDefence.ToString(),
                AccessRequestId = digitalEvidenceDefence.Id,
                OrganizationType = command.OrganizationType,
                OrganizationName = command.OrganizationName,
            };
        }

        private async Task<bool> CreateDisclosureDeferredPublishRequest(Command command, PartyDto party, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
        {
            Serilog.Log.Information($"Storing deferred request for publishing later if needed");

            var payload = JsonConvert.SerializeObject(this.GetDisclosureUserModel(command, party, digitalEvidenceDisclosure));


            this.context.DeferredEvents.Add(new Models.OutBoxEvent.DeferredEvent
            {
                EventType = "disclosure-user-creation",
                DateOccurred = this.clock.GetCurrentInstant(),
                RequestId = digitalEvidenceDisclosure.Id,
                Reason = "Approval Required",
                EventPayload = payload
            });

            var addedRows = await this.context.SaveChangesAsync();


            return addedRows > 0;
        }

        private async Task<bool> CreateDefenceDeferredPublishRequest(Command command, PartyDto party, Pidp.Models.DigitalEvidenceDefence digitalEvidenceDefence)
        {
            Serilog.Log.Information($"Storing deferred request for publishing later if needed");

            var payload = JsonConvert.SerializeObject(this.GetEdtPersonModel(command, party, digitalEvidenceDefence));

            this.context.DeferredEvents.Add(new Models.OutBoxEvent.DeferredEvent
            {
                EventType = "defence-person-creation",
                DateOccurred = this.clock.GetCurrentInstant(),
                RequestId = digitalEvidenceDefence.Id,
                Reason = "Approval Required",
                EventPayload = payload
            });

            var addedRows = await this.context.SaveChangesAsync();


            return addedRows > 0;
        }

        private async Task<DeliveryResult<string, EdtDisclosureUserProvisioning>> PublishDisclosureAccessRequest(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.DisclosureDefenceUserCreationTopic, command.ParticipantId, taskId);

            var systemType = digitalEvidenceDisclosure.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal)
                ? AccessTypeCode.DigitalEvidenceDisclosure.ToString()
                : AccessTypeCode.DigitalEvidence.ToString();


            // use UUIDs for topic keys
            var delivered = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.DisclosureDefenceUserCreationTopic,
                taskId,
                this.GetDisclosureUserModel(command, dto, digitalEvidenceDisclosure));

            return delivered;
        }

        /// <summary>
        /// Publish a request to the approval topic
        /// </summary>
        /// <param name="keycloakUser"></param>
        /// <param name="command"></param>
        /// <param name="dto"></param>
        /// <param name="reasonList"></param>
        /// <param name="accessRequests"></param>
        /// <returns></returns>
        /// <exception cref="AccessRequestException"></exception>
        private async Task<List<DeliveryResult<string, ApprovalRequestModel>>> PublishApprovalRequest(UserRepresentation keycloakUser, Command command, PartyDto dto, List<string> reasonList, List<AccessRequest> accessRequests)
        {

            if (accessRequests == null || accessRequests.Count == 0)
            {
                throw new AccessRequestException("No access requests passed to PublishApprovalRequest()");
            }

            var results = new List<DeliveryResult<string, ApprovalRequestModel>>();

            var taskId = Guid.NewGuid().ToString();
            var userId = dto?.Jpdid;
            var identityProvider = keycloakUser.FederatedIdentities.FirstOrDefault()?.IdentityProvider;

            if (userId == null || identityProvider == null)
            {
                throw new AccessRequestException($"Failed to determine userID or idp for user {dto.UserId}");
            }
            Serilog.Log.Logger.Information("Adding message to approval topic {0} {1} {2}", this.config.KafkaCluster.ApprovalCreationTopic, command.ParticipantId, taskId);

            var requests = new List<ApprovalAccessRequest>();
            foreach (var request in accessRequests)
            {
                requests.Add(new ApprovalAccessRequest
                {
                    AccessRequestId = request.Id,
                    RequestType = request.AccessTypeCode.ToString(),
                });
            }

            var identities = new List<PersonalIdentityModel>
            {
                // add bc service card identity
                new PersonalIdentityModel
                {
                    Source = "BCSC",
                    FirstName = keycloakUser.Attributes["BCPerID_first_name"] != null && keycloakUser.Attributes["BCPerID_first_name"].Length > 0 ? string.Join(",", keycloakUser.Attributes["BCPerID_first_name"]) : "Not found",
                    LastName = keycloakUser.Attributes["BCPerID_last_name"] != null && keycloakUser.Attributes["BCPerID_last_name"].Length > 0 ? string.Join(",", keycloakUser.Attributes["BCPerID_last_name"]) : "Not found",
                },
                // add bc law info
                new PersonalIdentityModel
                {
                    Source = "BCLaw",
                    FirstName = keycloakUser.FirstName,
                    LastName = keycloakUser.LastName,
                    EMail = dto.Email,

                }
            };


            // use UUIDs for topic keys
            results.Add(await this.approvalKafkaProducer.ProduceAsync(this.config.KafkaCluster.ApprovalCreationTopic, taskId, new ApprovalRequestModel
            {
                AccessRequests = requests,
                Reasons = reasonList,
                RequiredAccess = "Defence and Duty Counsel Access",
                Created = DateTime.Now,
                PersonalIdentities = identities,
                EMailAddress = dto.Email,
                Phone = dto.Phone,
                UserId = userId,
                IdentityProvider = identityProvider
            }));

            return results;
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

            var digitalEvidenceDisclosure = new DigitalEvidenceDisclosure
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
