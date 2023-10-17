namespace Pidp.Features.AccessRequests;

using System.Threading.Tasks;
using Common.Exceptions;
using DomainResults.Common;
using FluentValidation;
using NodaTime;
using Pidp.Data;
using Pidp.Infrastructure.HttpClients.Edt;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Models;
using Pidp.Models.UserInfo;

public class ValidateUser
{

    public class Command : ICommand<IDomainResult<UserValidationResponse>>
    {
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
        private readonly IEdtCoreClient edtService;
        private readonly IClock clock;
        private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
        private readonly PidpConfiguration configuration;

        public CommandHandler(PidpDbContext dbContext, IEdtCoreClient edtService, IClock clock, IKeycloakAdministrationClient keycloakAdministrationClient, PidpConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.edtService = edtService;
            this.configuration = configuration;
            this.clock = clock;
            this.keycloakAdministrationClient = keycloakAdministrationClient;
        }

        public async Task<IDomainResult<UserValidationResponse>> HandleAsync(Command command)
        {

            // store request
            using var transaction = this.dbContext.Database.BeginTransaction();
            try
            {

                var party = this.dbContext.Parties.First(p => p.Id == command.PartyId);

                if (party == null)
                {
                    throw new DIAMAuthException($"Party {command.PartyId} was not found - request ignored");
                }

                var keycloakUser = await this.keycloakAdministrationClient.GetUser(party.UserId);

                if (keycloakUser == null)
                {
                    throw new DIAMAuthException($"Keycloak user {command.PartyId} {party.UserId} was not found - request ignored");
                }

                // see if we've had previous requests
                var priorRequests = this.dbContext.PublicUserValidations.Where(x => x.PartyId == command.PartyId).OrderBy(x => x.Created).ToList();

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
                    return DomainResult.Success(response);

                }

                var validation = new PublicUserValidation
                {
                    Code = command.Code,
                    Created = this.clock.GetCurrentInstant(),
                    PartyId = command.PartyId,
                    IsValid = false
                };

                this.dbContext.PublicUserValidations.Add(validation);

                // get the user info from the EdtService
                var userInfoResponse = await this.edtService.GetPersonsByIdentifier("EdtExternalId", command.Code);



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

                                var edtDOBField = info.Fields.First(field => field.Name.Equals("Date of Birth"));
                                var d = DateTimeOffset.Parse(edtDOBField.Value);
                                var dateonly = d.Date.ToShortDateString();

                                // we'll check against keycloak user as this will be sync with BCSC
                                var keycloakDOB = keycloakUser.Attributes.First(attr => attr.Key.Equals("birthdate"));
                                if (keycloakDOB.Value != null)
                                {
                                    if (keycloakDOB.Value.Length == 1 && keycloakDOB.Value[0].Equals(dateonly) &&
                                     keycloakUser.FirstName.Equals(edtFirstName, StringComparison.OrdinalIgnoreCase) &&
                                     keycloakUser.LastName.Equals(edtLastName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Serilog.Log.Information($"User info matches {keycloakUser.LastName} ({edtLastName})");
                                        response.Validated = true;
                                        validation.IsValid = true;
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

                await this.dbContext.SaveChangesAsync();


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



