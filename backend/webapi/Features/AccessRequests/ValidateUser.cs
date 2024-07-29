namespace Pidp.Features.AccessRequests;

using System.Net;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.Kafka;
using Common.Models.EDT;
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
        public string Code { get; set; } = string.Empty;
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.PartyId).NotEmpty();
            this.RuleFor(x => x.Code).NotEmpty();
        }


    }

    public class CommandHandler(PidpDbContext dbContext,
        IEdtCoreClient edtCoreClient,
        IClock clock,
        IKeycloakAdministrationClient keycloakAdministrationClient,
        PidpConfiguration configuration,
        IKafkaProducer<string, Notification> kafkaNotificationProducer
            ) : ICommandHandler<Command, IDomainResult<UserValidationResponse>>
    {


        public async Task<IDomainResult<UserValidationResponse>> HandleAsync(Command command)
        {

            // see if this code is already active and complete
            var priorAccessRequest = dbContext.DigitalEvidencePublicDisclosures.Where(req => req.KeyData == command.Code).FirstOrDefault();

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

            // start a transaction
            using var transaction = dbContext.Database.BeginTransaction();
            try
            {

                var party = dbContext.Parties.First(p => p.Id == command.PartyId) ?? throw new DIAMAuthException($"Party {command.PartyId} was not found - request ignored");

                var keycloakUser = await keycloakAdministrationClient.GetUser(Common.Constants.Auth.RealmConstants.BCPSRealm, party.UserId) ?? throw new DIAMAuthException($"Keycloak user {command.PartyId} {party.UserId} was not found - request ignored");

                // see if we've had previous requests
                var priorRequests = dbContext.PublicUserValidations.Where(x => x.PartyId == command.PartyId && !x.IsValid).OrderBy(x => x.Created).ToList();

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

                if (retries >= configuration.EdtClient.MaxClientValidations)
                {
                    response.TooManyAttempts = true;
                    await this.NotifyTooManyAttempts(party, priorRequests);
                    return DomainResult.Success(response);

                }


                var validation = new PublicUserValidation
                {
                    Code = command.Code,
                    Created = clock.GetCurrentInstant(),
                    PartyId = command.PartyId,
                    IsValid = false
                };

                if (priorRequests != null && priorRequests.Count > 0)
                {
                    var sameCodeAttempt = priorRequests.FirstOrDefault(req => req.Party == party && req.Code == command.Code);
                    if (sameCodeAttempt != null)
                    {
                        Serilog.Log.Information($"User {party.Id} entered duplicated code");
                        sameCodeAttempt.Modified = clock.GetCurrentInstant();
                    }
                    else
                    {
                        dbContext.PublicUserValidations.Add(validation);

                    }
                }
                else
                {
                    dbContext.PublicUserValidations.Add(validation);
                }



                // get the user primaryParticipant from the EdtService
                EdtPersonDto? primaryParticipant = null;

                var userInfoResponse = await edtCoreClient.FindPersons(new PersonLookupModel()
                {
                    DateOfBirth = party.Birthdate,
                    FirstName = party.FirstName,
                    LastName = party.LastName,
                    AttributeValues = [
                        new()
                        {
                            Name = "OTC",
                            Value = command.Code,
                            ValType = LookupType.Field
                        }
                    ]
                });


                // see how many users we have - if more than one then we need to determine if they are merged but the same person
                // and merged users will be considered (even if inactive), so long as one of the persons has the right first, last names
                // date of birth and OTC then user will be allowed to proceed


                response = await this.GetPrimaryParticipant(userInfoResponse, command, party, keycloakUser, priorRequests);
                Serilog.Log.Information($"Primary participant {primaryParticipant?.Id} {primaryParticipant?.Key} {primaryParticipant?.FirstName} {primaryParticipant?.LastName} {primaryParticipant?.IsActive} {command.Code}");


                var updates = await dbContext.SaveChangesAsync();
                Serilog.Log.Debug($"{updates} public request records added");


                await transaction.CommitAsync();

                return DomainResult.Success(response);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Person validation error: {ex.Message}");
                await transaction.RollbackAsync();
                throw;

            }
        }


        /// <summary>
        /// Notify BCPS that a user has attempted too many times
        /// </summary>
        /// <param name="party"></param>
        /// <param name="priorRequests"></param>
        /// <returns></returns>
        /// <exception cref="DIAMGeneralException"></exception>
        private async Task NotifyTooManyAttempts(Party party, List<PublicUserValidation> priorRequests)
        {
            Serilog.Log.Warning($"Too many validation attempts by user {party.Id} {party.FirstName} {party.LastName}");

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
            var produceResponse = await kafkaNotificationProducer.ProduceAsync(configuration.KafkaCluster.NotificationTopicName, Guid.NewGuid().
                ToString(), new Notification
                {
                    DomainEvent = "digitalevidence-bcsc-too-many-validation-attempts",
                    To = configuration.EnvironmentConfig.SupportEmail,
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
                throw new DIAMGeneralException($"Failed to publish to topic for {party.Id} {party.Jpdid} - too many validation attempts");

            }
        }

        /// <summary>
        /// Get the primary participant - the person that will be linked in EDT
        /// </summary>
        /// <param name="userInfoResponse"></param>
        /// <param name="code"></param>
        /// <param name="party"></param>
        /// <param name="keycloakUser"></param>
        /// <returns></returns>
        private async Task<UserValidationResponse> GetPrimaryParticipant(List<EdtPersonDto>? userInfoResponse, Command command, Party party, UserRepresentation keycloakUser, List<PublicUserValidation> priorRequests)
        {

            EdtPersonDto? primaryParticipant = null;

            var response = new UserValidationResponse
            {
                Key = command.Code,
                PartyId = command.PartyId,
                Validated = false,
                Message = ""
            };


            if (userInfoResponse != null && userInfoResponse.Count > 0)
            {
                if (userInfoResponse.Count > 1)
                {
                    Serilog.Log.Error($"Multiple users found in EDT with {party.FirstName} {party.LastName} {party.Birthdate}");

                    // add code to find the active participant from the list
                    var activePersons = userInfoResponse.Where(person => person.IsActive).ToList();
                    if (activePersons.Count == 0)
                    {
                        Serilog.Log.Information($"No active users found with name {party.FirstName} {party.LastName} {party.Birthdate}");
                        response.Message = "No active participants found";
                    }
                    else if (activePersons.Count > 1)
                    {
                        Serilog.Log.Information($"Multiple active users found with name {party.FirstName} {party.LastName} {party.Birthdate} - if any match OTC {command.Code} that will be selected");

                        var activePerson = activePersons.FirstOrDefault(person => person.Fields.Any(field => field.Name.Equals(configuration.EdtClient.OneTimeCode) && field.Value.Equals(command.Code)));

                        if (activePerson == null)
                        {
                            Serilog.Log.Information($"No active users found with name {party.FirstName} {party.LastName} {party.Birthdate} - and OTC {command.Code}");
                            response.Message = "No participant found with OTC";

                        }
                        else
                        {
                            primaryParticipant = activePerson;
                        }

                    }

                }
                else // we have one response - check the OTC
                {


                    primaryParticipant = userInfoResponse.FirstOrDefault();



                    if (primaryParticipant != null)
                    {
                        var edtOTC = primaryParticipant.Fields.FirstOrDefault(field => field.Name.Equals(configuration.EdtClient.OneTimeCode, StringComparison.OrdinalIgnoreCase));

                        // Check if the OTC is populated
                        if (edtOTC == null || edtOTC.Value == null || string.IsNullOrEmpty(edtOTC.Value.ToString()))
                        {
                            var msg = $"One time code not populated in EDT for {command.PartyId}";
                            Serilog.Log.Warning(msg);
                            response.Message = msg;
                            return response;
                        }

                        var otcValid = edtOTC.Value.ToString().Equals(command.Code, StringComparison.OrdinalIgnoreCase);

                        if (!otcValid)
                        {
                            Serilog.Log.Information($"User {primaryParticipant.Id} {primaryParticipant.Key} did not match OTC {command.Code}");
                            response.Message = "Invalid code provided";
                            return response;
                        }

                        var edtFirstName = primaryParticipant.FirstName;
                        var edtLastName = primaryParticipant.LastName;
                        if (primaryParticipant.Fields.Any())
                        {
                            var edtDOBField = primaryParticipant.Fields.FirstOrDefault(field => field.Name.Equals(configuration.EdtClient.DateOfBirthField));


                            // Check if the DOB is populated
                            if (edtDOBField == null || edtDOBField.Value == null || string.IsNullOrEmpty(edtDOBField.Value.ToString()))
                            {
                                var msg = $"Date of birth not populated in EDT for {command.PartyId}";
                                Serilog.Log.Warning(msg);
                                response.Message = msg;
                            }
                            else
                            {



                                // we'll check against keycloak user as this will be sync with BCSC
                                var keycloakDOB = keycloakUser.Attributes.First(attr => attr.Key.Equals(configuration.Keycloak.BirthdateField));
                                if (keycloakDOB.Value != null)
                                {
                                    var keycloakDOBString = (keycloakDOB.Value.Length == 1) ? keycloakDOB.Value[0] : "";
                                    var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");

                                    var keyCloakDateOnly = DateOnly.Parse(keycloakDOBString);
                                    var edtDateOnly = DateOnly.Parse(edtDOBField.Value.ToString());

                                    var datesMatch = keyCloakDateOnly != null ? keyCloakDateOnly.CompareTo(edtDateOnly) == 0 : false;
                                    var firstNameMatch = keycloakUser.FirstName.Equals(edtFirstName, StringComparison.OrdinalIgnoreCase);
                                    var lastNameMatch = keycloakUser.LastName.Equals(edtLastName, StringComparison.OrdinalIgnoreCase);

                                    if (datesMatch && firstNameMatch && lastNameMatch)
                                    {
                                        Serilog.Log.Information($"User info matches {keycloakUser.LastName} ({edtLastName})");
                                        response.Validated = true;
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
                                                { "bcpsdob", edtDOBField.Value.ToString() },
                                                { "partyId", party.Jpdid},
                                                { "bcscLastName", party.LastName },
                                                { "ipAddress", command.IPAddress.ToString() },
                                                { "bcscdob",  keycloakDOB.Value[0] },
                                                { "codes", string.Join(",", codesTried) }
                                            };

                                            response.DataMismatch = true;
                                            response.Message = "Data mismatch";


                                            this.PublishDataMismatchNotification(party, eventData);


                                        }
                                    }
                                }

                            }

                        }

                    }
                    else
                    {
                        Serilog.Log.Error($"No fields found for {primaryParticipant.Id} {primaryParticipant.Key} - unable to check DOB");
                    }

                }

            }
            else
            {
                Serilog.Log.Information($"No users found with identifier {command.Code}");
                response.Message = "Invalid code provided";
            }


            return response;
        }


        /// <summary>
        /// Let BCPS users know about mismatched data
        /// </summary>
        /// <param name="party"></param>
        /// <param name="eventData"></param>
        /// <exception cref="DIAMGeneralException"></exception>
        private async void PublishDataMismatchNotification(Party party, Dictionary<string, string> eventData)
        {
            // send a notification to the message topic
            var produceResponse = await kafkaNotificationProducer.ProduceAsync(configuration.KafkaCluster.NotificationTopicName, Guid.NewGuid().ToString(), new Notification
            {
                DomainEvent = "digitalevidence-bcsc-data-mismatch",
                To = configuration.EnvironmentConfig.SupportEmail,
                Subject = $"BCSC DIAM User {party.LastName} EDT/JUSTIN info does not match credentials",
                EventData = eventData

            });

            if (produceResponse.Status == Confluent.Kafka.PersistenceStatus.Persisted)
            {
                Serilog.Log.Information($"Published notification for {party.LastName} data mismatch event");
            }
            else
            {
                throw new DIAMGeneralException("Failed to publish BCPS event - transaction will rollback");

            }
        }
    }
}



