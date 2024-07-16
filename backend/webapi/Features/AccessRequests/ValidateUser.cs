namespace Pidp.Features.AccessRequests;

using System.Net;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.Kafka;
using Common.Models.Notification;
using DomainResults.Common;
using FluentValidation;
using NodaTime;
using NodaTime.Text;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Models;
using Pidp.Models.UserInfo;

public class ValidateUser
{

    public class Command : ICommand<IDomainResult<UserValidationResponse>>
    {
        public IPAddress? IPAddress { get; set; }
        public int PartyId { get; set; }
        public string Code { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.PartyId).NotEmpty();
            this.RuleFor(x => x.Code).NotEmpty();
        }


    }

    public class CommandHandler : ICommandHandler<Command, IDomainResult<UserValidationResponse>>
    {
        private readonly PidpDbContext dbContext;
        private readonly IEdtCoreClient edtCoreClient;
        private readonly IClock clock;
        private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
        private readonly PidpConfiguration configuration;
        private readonly IKafkaProducer<string, Notification> kafkaNotificationProducer;

        public CommandHandler(PidpDbContext dbContext,
            IEdtCoreClient edtService,
            IClock clock,
            IKeycloakAdministrationClient keycloakAdministrationClient,
            PidpConfiguration configuration,
            IKafkaProducer<string, Notification> kafkaNotificationProducer
            )
        {
            this.dbContext = dbContext;
            this.edtCoreClient = edtService;
            this.configuration = configuration;
            this.clock = clock;
            this.keycloakAdministrationClient = keycloakAdministrationClient;
            this.kafkaNotificationProducer = kafkaNotificationProducer;
        }

        public async Task<IDomainResult<UserValidationResponse>> HandleAsync(Command command)
        {

            // see if this code is already active and complete
            var priorAccessRequest = this.dbContext.DigitalEvidencePublicDisclosures.Where(req => req.KeyData == command.Code).FirstOrDefault();

            if (priorAccessRequest != null)
            {
                Serilog.Log.Information($"User {command.PartyId} is attempting to use code {command.Code} previously submitted");
                return DomainResult.Success(new UserValidationResponse
                {
                    AlreadyActive = true,
                    Message = "You have already requested access using this code",
                    PartyId = command.PartyId,
                    RequestStatus = priorAccessRequest.Status,
                    Key = command.Code,
                    Validated = true
                });

            }

            // store request
            using var transaction = this.dbContext.Database.BeginTransaction();
            try
            {

                var party = this.dbContext.Parties.First(p => p.Id == command.PartyId);

                if (party == null)
                {
                    throw new DIAMAuthException($"Party {command.PartyId} was not found - request ignored");
                }

                var keycloakUser = await this.keycloakAdministrationClient.GetUser(Common.Constants.Auth.RealmConstants.BCPSRealm, party.UserId);

                if (keycloakUser == null)
                {
                    throw new DIAMAuthException($"Keycloak user {command.PartyId} {party.UserId} was not found - request ignored");
                }



                // see if we've had previous requests
                var priorRequests = this.dbContext.PublicUserValidations.Where(x => x.PartyId == command.PartyId && !x.IsValid).OrderBy(x => x.Created).ToList();

                var retries = 0;
                foreach (var attempt in priorRequests)
                {
                    if (attempt.IsValid)
                    {
                        retries = 0;
                    }
                    else
                    {
                        retries++;
                    }
                }

                var response = new UserValidationResponse
                {
                    Key = command.Code,
                    PartyId = command.PartyId,
                    Validated = false,
                    Message = ""

                };

                if (retries >= this.configuration.EdtClient.MaxClientValidations)
                {
                    Serilog.Log.Warning($"Too many validation attempts by user {party.Id} {party.FirstName} {party.LastName}");
                    response.TooManyAttempts = true;
                    await transaction.RollbackAsync();
                    var codesTried = priorRequests.Select(req => req.Code).ToList();
                    var eventData = new Dictionary<string, string>
                    {
                        { "attempts", "" + priorRequests.Count },
                        { "firstName", party.FirstName },
                        { "partyId", party.Jpdid},
                        { "lastName", party.LastName },
                        { "dob",  party.Birthdate.ToString() },
                        { "codes", string.Join(",", codesTried) }
                    };


                    // send a notification to the message topic
                    var produceResponse = await this.kafkaNotificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, Guid.NewGuid().
                        ToString(), new Notification
                        {
                            DomainEvent = "digitalevidence-bcsc-too-many-validation-attempts",
                            To = this.configuration.EnvironmentConfig.SupportEmail,
                            Subject = $"BCSC DIAM User {party.LastName} had too many code validation attempts",
                            EventData = eventData

                        });

                    if (produceResponse.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                    {
                        Serilog.Log.Information($"Published user {party.Id} {party.Jpdid} too many requests message");
                    }
                    else
                    {
                        Serilog.Log.Error($"Failed to publish to topic for {party.Id} {party.Jpdid} - too many validation attempts");
                    }
                    return DomainResult.Success(response);

                }


                var validation = new PublicUserValidation
                {
                    Code = command.Code,
                    Created = this.clock.GetCurrentInstant(),
                    PartyId = command.PartyId,
                    IsValid = false
                };

                if (priorRequests != null && priorRequests.Any())
                {
                    var sameCodeAttempt = priorRequests.Where(req => req.Party == party && req.Code == command.Code).FirstOrDefault();
                    if (sameCodeAttempt != null)
                    {
                        Serilog.Log.Information($"User {party.Id} entered duplicated code");
                        sameCodeAttempt.Modified = this.clock.GetCurrentInstant();
                    }
                    else
                    {
                        this.dbContext.PublicUserValidations.Add(validation);

                    }
                }
                else
                {
                    this.dbContext.PublicUserValidations.Add(validation);
                }



                // get the user info from the EdtService
                var userInfoResponse = await this.edtCoreClient.GetPersonsByIdentifier("EdtExternalId", command.Code);

                if (userInfoResponse != null && userInfoResponse.Count > 0)
                {
                    if (userInfoResponse.Count > 1)
                    {
                        Serilog.Log.Error($"Multiple users found in EDT with code {command.Code}");
                    }
                    else
                    {
                        var info = userInfoResponse.FirstOrDefault();
                        if (info != null)
                        {
                            var edtFirstName = info.FirstName;
                            var edtLastName = info.LastName;
                            if (info.Fields.Any())
                            {
                                var edtDOBField = info.Fields.First(field => field.Name.Equals(this.configuration.EdtClient.DateOfBirthField));
                                if (edtDOBField == null)
                                {
                                    var msg = $"Date of birth not populated in EDT for {command.PartyId}";
                                    Serilog.Log.Warning(msg);
                                    response.Message = msg;
                                }
                                else
                                {
                                    if (edtDOBField.Value is string)
                                    {
                                        var DOBFieldString = edtDOBField.Value.ToString();
                                        var edtDOBDateOnly = (DOBFieldString.Contains(" ")) ? DateOnly.Parse(DOBFieldString.Split(" ")[0]) : DateOnly.Parse(DOBFieldString);

                                        // we'll check against keycloak user as this will be sync with BCSC
                                        var keycloakDOB = keycloakUser.Attributes.First(attr => attr.Key.Equals(this.configuration.Keycloak.BirthdateField));
                                        if (keycloakDOB.Value != null)
                                        {
                                            var keycloakDOBString = (keycloakDOB.Value.Length == 1) ? keycloakDOB.Value[0] : "";
                                            var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");

                                            var keyCloakDateOnly = DateOnly.Parse(keycloakDOBString);
                                            var datesMatch = keyCloakDateOnly != null ? keyCloakDateOnly.CompareTo(edtDOBDateOnly) == 0 : false;
                                            var firstNameMatch = keycloakUser.FirstName.Equals(edtFirstName, StringComparison.OrdinalIgnoreCase);
                                            var lastNameMatch = keycloakUser.LastName.Equals(edtLastName, StringComparison.OrdinalIgnoreCase);

                                            if (datesMatch && firstNameMatch && lastNameMatch)
                                            {
                                                Serilog.Log.Information($"User info matches {keycloakUser.LastName} ({edtLastName})");
                                                response.Validated = true;
                                                validation.IsValid = true;
                                            }
                                            else
                                            {
                                                // if at least one item matches then we'll notify BCPS
                                                if (firstNameMatch || lastNameMatch || datesMatch)
                                                {
                                                    Serilog.Log.Information($"User {keycloakUser.FirstName} {keycloakUser.LastName} was a potential match [FN={firstNameMatch}] [LN={lastNameMatch}] [DOB={datesMatch}] with one or more issues found");
                                                    // publish a message for BCPS
                                                    var codesTried = priorRequests.Select(req => req.Code).ToList();

                                                    var eventData = new Dictionary<string, string>
                                            {
                                                { "attempts", "" + priorRequests.Count },
                                                { "bcscFirstName", party.FirstName },
                                                { "bcpsFirstName", edtFirstName },
                                                { "bcpsLastName", edtLastName },
                                                { "requestDateTime", DateTime.Now.ToString() },
                                                { "bcpsdob", edtDOBDateOnly.ToString() },
                                                { "partyId", party.Jpdid},
                                                { "bcscLastName", party.LastName },
                                                { "ipAddress", command.IPAddress.ToString() },
                                                { "bcscdob",  keycloakDOB.Value[0] },
                                                { "codes", string.Join(",", codesTried) }
                                            };

                                                    response.DataMismatch = true;
                                                    response.Message = "Data mismatch";

                                                    // send a notification to the message topic
                                                    var produceResponse = await this.kafkaNotificationProducer.ProduceAsync(this.configuration.KafkaCluster.NotificationTopicName, Guid.NewGuid().
                                                        ToString(), new Notification
                                                        {
                                                            DomainEvent = "digitalevidence-bcsc-data-mismatch",
                                                            To = this.configuration.EnvironmentConfig.SupportEmail,
                                                            Subject = $"BCSC DIAM User {party.LastName} EDT/JUSTIN info does not match credentials",
                                                            EventData = eventData

                                                        });

                                                    if (produceResponse.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                                                    {
                                                        Serilog.Log.Information($"Published notification for {party.LastName} data mismatch event");
                                                    }
                                                    else
                                                    {
                                                        await transaction.RollbackAsync();
                                                        throw new DIAMGeneralException("Failed to publish BCPS event - transaction will rollback");
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }

                            }

                        }
                        else
                        {
                            Serilog.Log.Error($"No fields found for {info.Id} {info.Key} - unable to check DOB");
                        }

                    }

                }
                else
                {
                    Serilog.Log.Information($"No users found with identifier {command.Code}");
                    response.Message = "Invalid code provided";
                }

                var updates = await this.dbContext.SaveChangesAsync();
                Serilog.Log.Debug($"{updates} public request records added");


                await transaction.CommitAsync();

                return DomainResult.Success(response);
            }
            catch (Exception ex)
            {
                throw new DIAMGeneralException(ex.Message, ex);

            }
        }


    }
}



