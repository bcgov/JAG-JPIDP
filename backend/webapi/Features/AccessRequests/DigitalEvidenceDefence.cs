namespace Pidp.Features.AccessRequests;

using common.Constants.Auth;
using Common.Models.Approval;
using Common.Models.EDT;
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
using System.Diagnostics;
using System.Linq;

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

      RuleFor(x => x.OrganizationName).NotEmpty();
      RuleFor(x => x.OrganizationType).NotEmpty();
      RuleFor(x => x.ParticipantId).NotEmpty();
      RuleFor(x => x.PartyId).GreaterThan(0);

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
    private readonly IKafkaProducer<string, EdtDisclosureDefenceUserProvisioningModel> kafkaProducer;
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
    IKafkaProducer<string, EdtDisclosureDefenceUserProvisioningModel> kafkaProducer,
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
      using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction trx = context.Database.BeginTransaction();

      using (Activity activity = new Activity("DigitalEvidenceDefence Request").Start())
      {


        try
        {

          ActivityTraceId traceId = Tracer.CurrentSpan.Context.TraceId;
          Serilog.Log.Logger.Information($"DigitalEvidenceDefence Request {command.ParticipantId}  {command.PartyId} trace: [{traceId}]");

          Activity.Current?.AddTag("digitalevidence.party.id", command.PartyId);

          PartyDto dto = await GetPidpUser(command);

          if (dto.AlreadyEnroled
              || dto.Email == null)
          {
            Serilog.Log.Logger.Warning($"DigitalEvidence Request denied for user {command.PartyId} Enrolled {dto.AlreadyEnroled}, Email {dto.Email}");
            logger.LogDigitalEvidenceDisclosureAccessRequestDenied();
            await trx.RollbackAsync();

            return DomainResult.Failed();
          }

          // get the user details from keycloak and check they are valid - otherwise will require an approval step
          UserRepresentation? keycloakUser = await keycloakClient.GetUser(dto.UserId);

          if (keycloakUser == null)
          {
            await trx.RollbackAsync();

            throw new AccessRequestException($"No keycloak user found with id {dto.UserId}");
          }

          List<string> userValidationErrors = IsKeycloakUserValid(keycloakUser);

          // create db entry for disclosure access
          DigitalEvidenceDisclosure disclosureUser = await SubmitDigitalEvidenceDisclosureRequest(command);
          // create entry for defence (core) access
          Models.DigitalEvidenceDefence defenceUser = await SubmitDigitalEvidenceDefenceRequest(command);

          string? permitMismatchedVCCreds = Environment.GetEnvironmentVariable("PERMIT_MISMATCH_VC_CREDS");
          bool ignoreBCServiceCard = false;
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
            List<DeliveryResult<string, ApprovalRequestModel>> deliveryResult = await PublishApprovalRequest(keycloakUser, command, dto, userValidationErrors, new List<Models.AccessRequest> { defenceUser, disclosureUser });

            foreach (DeliveryResult<string, ApprovalRequestModel> result in deliveryResult)
            {
              if (result.Status == PersistenceStatus.Persisted)
              {
                Serilog.Log.Information($"Published {result.Key} to {result.Partition}");

                // store the original requests for later
                if (await CreateDefenceDeferredPublishRequest(command, dto, defenceUser) && await CreateDisclosureDeferredPublishRequest(command, dto, disclosureUser))
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
            DeliveryResult<string, EdtDisclosureDefenceUserProvisioningModel> publishedDisclosureRequest = await PublishDisclosureAccessRequest(command, dto, disclosureUser);

            // publish message for disclosure access
            DeliveryResult<string, EdtPersonProvisioningModel> publishedDefenceRequest = await PublishDefenceAccessRequest(command, dto, defenceUser);

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

          await context.SaveChangesAsync();
          await trx.CommitAsync();

          return DomainResult.Success();


        }
        catch (Exception ex)
        {
          logger.LogDigitalEvidenceAccessTrxFailed(ex.Message.ToString());
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
      bool hasFamilyName = keycloakUser.Attributes.TryGetValue(Claims.BcPersonFamilyName, out string[]? BCFamilyName);
      bool hasFirstName = keycloakUser.Attributes.TryGetValue(Claims.BcPersonGivenName, out string[]? BCFirstName);
      bool hasMemberStatus = keycloakUser.Attributes.TryGetValue(Claims.MembershipStatusCode, out string[]? memberShipStatus);
      List<string> errors = new List<string>();

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
      string taskId = Guid.NewGuid().ToString();
      Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", config.KafkaCluster.PersonCreationTopic, command.ParticipantId, taskId);

      // use UUIDs for topic keys
      DeliveryResult<string, EdtPersonProvisioningModel> delivered = await kafkaDefenceCoreProducer.ProduceAsync(config.KafkaCluster.PersonCreationTopic, taskId, GetEdtPersonModel(command, dto, digitalEvidenceDefence));

      return delivered;
    }

    private EdtDisclosureDefenceUserProvisioningModel GetDisclosureUserModel(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
    {
      string systemType = digitalEvidenceDisclosure.OrganizationType.Equals(LAW_SOCIETY, StringComparison.Ordinal) ? AccessTypeCode.DigitalEvidenceDisclosure.ToString() : AccessTypeCode.DigitalEvidence.ToString();


      return new EdtDisclosureDefenceUserProvisioningModel
      {
        Key = $"{command.ParticipantId}",
        UserName = dto.Jpdid,
        Email = dto.Email,
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
        Fields = new List<Models.EdtField> { },
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

      string payload = JsonConvert.SerializeObject(GetDisclosureUserModel(command, party, digitalEvidenceDisclosure));


      context.DeferredEvents.Add(new Models.OutBoxEvent.DeferredEvent
      {
        EventType = "disclosure-user-creation",
        DateOccurred = clock.GetCurrentInstant(),
        RequestId = digitalEvidenceDisclosure.Id,
        Reason = "Approval Required",
        EventPayload = payload
      });

      int addedRows = await context.SaveChangesAsync();


      return addedRows > 0;
    }

    private async Task<bool> CreateDefenceDeferredPublishRequest(Command command, PartyDto party, Pidp.Models.DigitalEvidenceDefence digitalEvidenceDefence)
    {
      Serilog.Log.Information($"Storing deferred request for publishing later if needed");

      string payload = JsonConvert.SerializeObject(GetEdtPersonModel(command, party, digitalEvidenceDefence));

      context.DeferredEvents.Add(new Models.OutBoxEvent.DeferredEvent
      {
        EventType = "defence-person-creation",
        DateOccurred = clock.GetCurrentInstant(),
        RequestId = digitalEvidenceDefence.Id,
        Reason = "Approval Required",
        EventPayload = payload
      });

      int addedRows = await context.SaveChangesAsync();


      return addedRows > 0;
    }

    private async Task<DeliveryResult<string, EdtDisclosureDefenceUserProvisioningModel>> PublishDisclosureAccessRequest(Command command, PartyDto dto, DigitalEvidenceDisclosure digitalEvidenceDisclosure)
    {
      string taskId = Guid.NewGuid().ToString();
      Serilog.Log.Logger.Information("Adding message to topic {0} {1} {2}", config.KafkaCluster.DisclosureDefenceUserCreationTopic, command.ParticipantId, taskId);

      string systemType = digitalEvidenceDisclosure.OrganizationType.Equals(LAW_SOCIETY, StringComparison.Ordinal)
                ? AccessTypeCode.DigitalEvidenceDisclosure.ToString()
                : AccessTypeCode.DigitalEvidence.ToString();


      // use UUIDs for topic keys
      DeliveryResult<string, EdtDisclosureDefenceUserProvisioningModel> delivered = await kafkaProducer.ProduceAsync(config.KafkaCluster.DisclosureDefenceUserCreationTopic,
          taskId,
          GetDisclosureUserModel(command, dto, digitalEvidenceDisclosure));

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

      List<DeliveryResult<string, ApprovalRequestModel>> results = new List<DeliveryResult<string, ApprovalRequestModel>>();

      string taskId = Guid.NewGuid().ToString();
      string? userId = dto?.Jpdid;
      string? identityProvider = keycloakUser.FederatedIdentities.FirstOrDefault()?.IdentityProvider;

      if (userId == null || identityProvider == null)
      {
        throw new AccessRequestException($"Failed to determine userID or idp for user {dto.UserId}");
      }
      Serilog.Log.Logger.Information("Adding message to approval topic {0} {1} {2}", config.KafkaCluster.ApprovalCreationTopic, command.ParticipantId, taskId);

      List<ApprovalAccessRequest> requests = new List<ApprovalAccessRequest>();
      foreach (AccessRequest request in accessRequests)
      {
        requests.Add(new ApprovalAccessRequest
        {
          AccessRequestId = request.Id,
          RequestType = request.AccessTypeCode.ToString(),
        });
      }

      List<PersonalIdentityModel> identities = new List<PersonalIdentityModel>
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
      results.Add(await approvalKafkaProducer.ProduceAsync(config.KafkaCluster.ApprovalCreationTopic, taskId, new ApprovalRequestModel
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

      Models.DigitalEvidenceDefence digitalEvidenceDefence = new Models.DigitalEvidenceDefence
      {
        PartyId = command.PartyId,
        Status = AccessRequestStatus.Pending,
        ParticipantId = command.ParticipantId,
        AccessTypeCode = AccessTypeCode.DigitalEvidenceDefence,
        RequestedOn = clock.GetCurrentInstant(),
      };
      context.DigitalEvidenceDefences.Add(digitalEvidenceDefence);

      await context.SaveChangesAsync();
      return digitalEvidenceDefence;
    }

    private async Task<DigitalEvidenceDisclosure> SubmitDigitalEvidenceDisclosureRequest(Command command)
    {

      DigitalEvidenceDisclosure digitalEvidenceDisclosure = new DigitalEvidenceDisclosure
      {
        PartyId = command.PartyId,
        Status = AccessRequestStatus.Pending,
        OrganizationType = command.OrganizationType.ToString(),
        OrganizationName = command.OrganizationName,
        ParticipantId = command.ParticipantId,
        AccessTypeCode = AccessTypeCode.DigitalEvidenceDisclosure,
        RequestedOn = clock.GetCurrentInstant(),
      };
      context.DigitalEvidenceDisclosures.Add(digitalEvidenceDisclosure);

      await context.SaveChangesAsync();
      return digitalEvidenceDisclosure;
    }

    private async Task<PartyDto> GetPidpUser(Command command)
    {
      return await context.Parties
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
