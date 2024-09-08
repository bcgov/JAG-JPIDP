namespace Pidp.Features.AccessRequests;

using System.Diagnostics;
using System.Linq;
using common.Constants.Auth;
using Common.Models.Approval;
using Common.Models.EDT;
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
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;
using Prometheus;

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
        private readonly IEdtCoreClient coreClient;
        private readonly IKafkaProducer<string, EdtDisclosureDefenceUserProvisioningModel> kafkaProducer;
        private readonly IKafkaProducer<string, ApprovalRequestModel> approvalKafkaProducer;
        private static readonly Histogram DefenceCommandHistogram = Metrics.CreateHistogram("defence_command_timing", "Histogram of defence command executions.");
        private static readonly char[] Separators = [' ', '-'];
        private readonly IKafkaProducer<string, EdtPersonProvisioningModel> kafkaDefenceCoreProducer;

        private readonly string LAW_SOCIETY = "LawSociety";

        // EdtDisclosureUserProvisioning
        // we'll track the request in access requests and then push the request to a topic for disclosure service to handle
        public CommandHandler(
            IClock clock,
            IKeycloakAdministrationClient keycloakClient,
            ILogger<CommandHandler> logger,
            PidpConfiguration config,
            IEdtCoreClient coreClient,
            PidpDbContext context,
            IKafkaProducer<string, EdtDisclosureDefenceUserProvisioningModel> kafkaProducer,
            IKafkaProducer<string, ApprovalRequestModel> approvalKafkaProducer,
            IKafkaProducer<string, EdtPersonProvisioningModel> kafkaDefenceCoreProducer)
        {
            this.clock = clock;
            this.keycloakClient = keycloakClient;
            this.logger = logger;
            this.coreClient = coreClient;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.approvalKafkaProducer = approvalKafkaProducer;
            this.kafkaDefenceCoreProducer = kafkaDefenceCoreProducer;
            this.config = config;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using var trx = this.context.Database.BeginTransaction();


            using (var activity = new Activity("DigitalEvidenceDefence Request").Start())
            {

                using (DefenceCommandHistogram.NewTimer())
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

                        // check if this user already exists as a defence participant in code
                        var existingParticipant = await this.coreClient.GetPersonByKey(dto.Email);

                        if (existingParticipant != null)
                        {
                            userValidationErrors = ValidateExistingParticipant(dto, existingParticipant);
                            defenceUser.ManuallyAddedParticipantId = (int)existingParticipant.Id;
                            var externalId = existingParticipant.Identifiers.FirstOrDefault(identifier => identifier.IdentifierType == "EdtExternalId");
                            if (externalId != null)
                            {
                                defenceUser.EdtExternalIdentifier = externalId.IdentifierValue;
                            }
                            Serilog.Log.Information($"Participant for defence {command.ParticipantId} {dto.Email} exists with external id {externalId.IdentifierValue}");

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
                                    if (await this.CreateDefenceDeferredPublishRequest(command, dto, defenceUser) && await this.CreateDisclosureDeferredPublishRequest(command, dto, disclosureUser, defenceUser))
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
                            var publishedDisclosureRequest = await this.PublishDisclosureAccessRequest(command, dto, disclosureUser, defenceUser);

                            // publish message for defence participa access
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
                        return DomainResult.Failed(ex.Message);
                    }
                }

            }
        }

        private static List<string> ValidateExistingParticipant(PartyDto dto, EdtPersonDto existingParticipant)
        {
            var errors = new List<string>();
            Serilog.Log.Information($"Validating user information is correct for exsting core person {dto.Email} - {existingParticipant.Id}");
            if (!dto.FirstName.Trim().Equals(existingParticipant.FirstName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Existing core defence user first name incorrect [{dto.FirstName}] [{existingParticipant.FirstName}]");
            }
            if (!dto.LastName.Trim().Equals(existingParticipant.LastName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Existing core defence user last name incorrect [{dto.LastName}] [{existingParticipant.LastName}]");
            }

            // todo - compare role once this is set in EDT

            return errors;

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

            if (errors.Count > 0)
            {
                // drop out and return errors
                return errors;
            }


            var BCFamilyNames = BCFamilyName[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var BCFirstNames = BCFirstName[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var BCLawFamilyNames = keycloakUser.LastName.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var BCLawFirstNames = keycloakUser.FirstName.Split(Separators, StringSplitOptions.RemoveEmptyEntries);



            if (BCFamilyNames.Length > 1 || BCLawFamilyNames.Length > 1)
            {
                Serilog.Log.Information($"Multiple family names found in claims [{string.Join(" ", BCFamilyNames)}]:[{string.Join(" ", BCLawFamilyNames)}] - only first will be compared");

            }
            if (!string.Equals(BCLawFamilyNames[0].Trim(), BCFamilyNames[0].Trim(), StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Error($"User family name does not match between BCSC [{string.Join(" ", BCFamilyNames)}] and BCLaw [{string.Join(" ", BCLawFamilyNames)}]");
                errors.Add($"User family name does not match between BCSC [{string.Join(" ", BCFamilyNames)}] and BCLaw [{string.Join(" ", BCLawFamilyNames)}]");
            }


            if (BCFirstNames.Length > 1 || BCLawFirstNames.Length > 1)
            {
                Serilog.Log.Logger.Information($"Multiple first names found in claims [{string.Join(" ", BCFirstNames)}]:[{string.Join(" ", BCLawFirstNames)}] - only first will be compared");
            }


            if (!string.Equals(BCFirstNames[0].Trim(), BCLawFirstNames[0].Trim(), StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Error($"User first name does not match between BCSC [{string.Join(" ", BCFirstNames)}] and BCLaw [{string.Join(" ", BCLawFirstNames)}]");
                errors.Add($"User first name does not match between BCSC [{string.Join(" ", BCFirstNames)}] and BCLaw [{string.Join(" ", BCLawFirstNames)}]");
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

        private EdtDisclosureDefenceUserProvisioningModel GetDisclosureUserModel(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure, Models.DigitalEvidenceDefence digitalEvidenceDefence
            )
        {
            var systemType = digitalEvidenceDisclosure.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal) ? AccessTypeCode.DigitalEvidenceDisclosure.ToString() : AccessTypeCode.DigitalEvidence.ToString();


            return new EdtDisclosureDefenceUserProvisioningModel
            {
                Key = $"{command.ParticipantId}",
                UserName = dto.Jpdid,
                Email = dto.Email,
                FullName = $"{dto.FirstName} {dto.LastName}",
                AccountType = "Saml",
                Role = "User",
                SystemName = systemType,
                Telephone = dto.Phone,
                AccessRequestId = digitalEvidenceDisclosure.Id,
                ManuallyAddedParticipantId = digitalEvidenceDefence.ManuallyAddedParticipantId,
                EdtExternalIdentifier = digitalEvidenceDefence.EdtExternalIdentifier,
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
                    Email = dto.Email ?? "Not set",
                    Phone = dto.Phone ?? "",
                },
                Fields = new List<EdtField> { },
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = "Participant",
                SystemName = AccessTypeCode.DigitalEvidenceDefence.ToString(),
                AccessRequestId = digitalEvidenceDefence.Id,
                ManuallyAddedParticipantId = digitalEvidenceDefence.ManuallyAddedParticipantId,
                EdtExternalIdentifier = digitalEvidenceDefence.EdtExternalIdentifier,
                OrganizationType = command.OrganizationType,
                OrganizationName = command.OrganizationName,
            };
        }

        private async Task<bool> CreateDisclosureDeferredPublishRequest(Command command, PartyDto party, Models.DigitalEvidenceDisclosure digitalEvidenceDisclosure, Models.DigitalEvidenceDefence digitalEvidenceDefence)
        {
            Serilog.Log.Information($"Storing deferred request for publishing later if needed");

            var payload = JsonConvert.SerializeObject(this.GetDisclosureUserModel(command, party, digitalEvidenceDisclosure, digitalEvidenceDefence));


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

        private async Task<DeliveryResult<string, EdtDisclosureDefenceUserProvisioningModel>> PublishDisclosureAccessRequest(Command command, PartyDto dto, Models.DigitalEvidenceDisclosure digitalEvidenceDisclosure, Models.DigitalEvidenceDefence defenceUser)
        {
            var taskId = Guid.NewGuid().ToString();
            Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", this.config.KafkaCluster.DisclosureDefenceUserCreationTopic, command.ParticipantId, taskId);

            var systemType = digitalEvidenceDisclosure.OrganizationType.Equals(this.LAW_SOCIETY, StringComparison.Ordinal)
                      ? AccessTypeCode.DigitalEvidenceDisclosure.ToString()
                      : AccessTypeCode.DigitalEvidence.ToString();


            // use UUIDs for topic keys
            var delivered = await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.DisclosureDefenceUserCreationTopic,
                taskId,
                this.GetDisclosureUserModel(command, dto, digitalEvidenceDisclosure, defenceUser));

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

            var reasonsList = new List<ApprovalRequestReason>();
            foreach (var reason in reasonList)
            {
                reasonsList.Add(new ApprovalRequestReason
                {
                    Reason = reason
                });
            }

            // use UUIDs for topic keys
            results.Add(await this.approvalKafkaProducer.ProduceAsync(this.config.KafkaCluster.ApprovalCreationTopic, taskId, new ApprovalRequestModel
            {
                AccessRequests = requests,
                Reasons = reasonsList,
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

    [LoggerMessage(3, LogLevel.Error, "User first name does not match between BCSC [{bcscFirstName}] and BCLaw [{bclawFirstName}] {keycloakUser}]")]
    public static partial void LogUserFirstNameMismatch(this ILogger logger, string bcscFirstName, string bclawFirstName, string keycloakUser);
}
