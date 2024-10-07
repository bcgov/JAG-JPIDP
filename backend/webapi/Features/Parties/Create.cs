namespace Pidp.Features.Parties;

using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pidp.Data;
using Pidp.Extensions;
using Pidp.Infrastructure.Auth;
using Pidp.Models;
using Pidp.Models.Lookups;
using Pidp.Models.UserInfo;

public class Create
{
    public class Command : ICommand<int>
    {
        public Guid UserId { get; set; }
        public string? Jpdid { get; set; }
        public DateOnly? Birthdate { get; set; }
        public string? Gender { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

    }

    public class CommandValidator : AbstractValidator<Command>
    {

        public CommandValidator(PidpDbContext context, IHttpContextAccessor accessor, PidpConfiguration config)
        {
            var user = accessor?.HttpContext?.User;

            this.RuleFor(x => x.UserId).NotEmpty().Equal(user.GetUserId());
            this.RuleFor(x => x.FirstName).NotEmpty().MatchesUserClaim(user, Claims.GivenName);
            this.RuleFor(x => x.LastName).NotEmpty().MatchesUserClaim(user, Claims.FamilyName);


            // get the SubmittingAgencies list from the result
            var submittingAgencies = this.GetSubmittingAgencies(context);

            // see if user is a member of a submitting agency
            var idp = user.GetIdentityProvider();

            var agency = submittingAgencies.Find(agency => agency.IdpHint.Equals(idp, StringComparison.Ordinal));

            if (agency != null)
            {
                Serilog.Log.Information("User {0} is from agency {1}", user.GetUserId(), agency.Name);
            }
            else
            {
                this.Include<AbstractValidator<Command>>(x =>
                {
                    // we need to quit hardcoding this stuff :-(
                    return user.GetIdentityProvider() switch
                    {
                        ClaimValues.BCServicesCard => new BcscValidator(user),
                        ClaimValues.Phsa => new PhsaValidator(),
                        ClaimValues.Bcps => new BcpsValidator(user),
                        ClaimValues.Idir => new IdirValidator(user),
                        ClaimValues.AzureAd => new IdirValidator(user),
                        ClaimValues.VerifiedCredentials => new VerifiedCredentialsValidator(user, config),
                        _ => throw new NotImplementedException($"Given Identity Provider {user.GetIdentityProvider()} is not supported")
                    };
                });
            }
        }

        public List<SubmittingAgency> GetSubmittingAgencies(PidpDbContext context)
        {
            // create an instance of the Query class
            var query = new Lookups.Index.Query();

            // create an instance of the QueryHandler class
            var handler = new Lookups.Index.QueryHandler(context);

            // execute the query and get the result
            var result = handler.HandleAsync(query);

            // get the SubmittingAgencies list from the result
            return result.Result.SubmittingAgencies;
        }

        private class BcscValidator : AbstractValidator<Command>
        {
            public BcscValidator(ClaimsPrincipal? user)
            {
                this.RuleFor(x => x.Jpdid).NotEmpty().MatchesUserClaim(user, Claims.PreferredUsername);
                this.RuleFor(x => x.Birthdate).NotEmpty().Equal(user?.GetBirthdate()).WithMessage($"Must match the \"birthdate\" Claim on the current User");
            }
        }
        private class BcpsValidator : AbstractValidator<Command>
        {
            public BcpsValidator(ClaimsPrincipal? user)
            {
                //this.RuleFor(x => x.Hpdid).Empty();
                this.RuleFor(x => x.Birthdate).Empty();
                this.RuleFor(x => x.Jpdid).NotEmpty().MatchesUserClaim(user, Claims.PreferredUsername);
                //this.RuleFor(x => x.Roles).NotEmpty().Contains("");
                //this.RuleFor(x => x.Birthdate).NotEmpty().Equal(user?.GetBirthdate()).WithMessage($"Must match the \"birthdate\" Claim on the current User");
            }
        }

        private class PhsaValidator : AbstractValidator<Command>
        {
            public PhsaValidator()
            {
                this.RuleFor(x => x.Jpdid).Empty();
                this.RuleFor(x => x.Birthdate).Empty();
            }
        }

        private class IdirValidator : AbstractValidator<Command>
        {
            public IdirValidator(ClaimsPrincipal? user)
            {
                this.RuleFor(x => x.Jpdid).NotEmpty().MatchesUserClaim(user, Claims.PreferredUsername);
                this.RuleFor(x => x.Birthdate).Empty();
            }
        }
        private class VerifiedCredentialsValidator : AbstractValidator<Command>
        {
            public VerifiedCredentialsValidator(ClaimsPrincipal? user, PidpConfiguration config)
            {

                //this.RuleFor(x => x.Jpdid).NotEmpty().MatchesUserClaim(user, Claims.PreferredUsername);
                //this.RuleFor(x => x.LastName).NotEmpty().MatchesUserClaim(user, Claims.BcPersonFamilyName);
                // this.RuleFor(x => "PRAC").MatchesUserClaim(user, Claims.MembershipStatusCode);
                //this.RuleFor(x => config.VerifiableCredentials.PresentedRequestId).MatchesUserClaim(user, Claims.VerifiedCredPresentedRequestId);
            }
        }
    }

    public class CommandHandler : ICommandHandler<Command, int>
    {
        private readonly PidpDbContext context;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CommandHandler(PidpDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> HandleAsync(Command command)
        {
            var user = this.httpContextAccessor.HttpContext.User;

            // check party isnt already present - should not happen though
            var party = this.context.Parties.Where(party => party.Jpdid == command.Jpdid).FirstOrDefault();

            if (party != null)
            {
                Serilog.Log.Warning($"Party is already present {command.Jpdid} - skipping add");
            }
            else
            {

                Serilog.Log.Information($"Adding new party {command.UserId} with id {command.Jpdid}");

                // if email is invalid (e.g. abc123@vc) then we'll null it here so the user needs
                // to update it to a valid email
                var email = command.Email != null && command.Email.EndsWith("@vc", StringComparison.OrdinalIgnoreCase) ? null : command.Email;
                if (email == null)
                {
                    Serilog.Log.Information($"User {command.Jpdid} is logged in with verifiable creds - setting null email address");
                }

                party = new Party
                {
                    UserId = command.UserId,
                    Jpdid = command.Jpdid,
                    Gender = command.Gender,
                    Birthdate = command.Birthdate,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Email = email
                };

                this.context.Parties.Add(party);
            }

            // if user is public user (BCSC) we'll set the org as public user
            if (user.GetIdentityProvider().Equals(ClaimValues.BCServicesCard))
            {
                Serilog.Log.Information("User {0} is using BCSC - automatically assigning organization", user.GetUserId());

                var outOfCustodyType = await this.GetAndAddUserTypeLookup(PublicUserType.OutOfCustodyAccused.ToString(), "Out of custody accused");

                if (outOfCustodyType != null)
                {
                    var partyUserType = new PartyUserType
                    {
                        Party = party,
                        UserTypeLookup = outOfCustodyType
                    };
                    party.PartyUserTypes.Add(partyUserType);
                }


            }

            if (!(user.GetIdentityProvider().Equals(ClaimValues.Bcps, StringComparison.Ordinal) || user.GetIdentityProvider().Equals(ClaimValues.Idir, StringComparison.Ordinal)))
            {

                if (user.GetIdentityProvider() == ClaimValues.VerifiedCredentials)
                {
                    Serilog.Log.Information("User {0} is using verified credentials - automatically assigning organization", user.GetUserId());
                    var lawSociety = this.GetOrganization(this.context, ClaimValues.VerifiedCredentials);
                    // right now we only have one org using verified credentials - this might change in future
                    if (lawSociety == null)
                    {
                        Serilog.Log.Information("No organization exists for law society - adding new entry");
                        lawSociety = new Organization
                        {
                            IdpHint = ClaimValues.VerifiedCredentials,
                            Name = "BC Law Society"
                        };
                        this.context.Organizations.Add(lawSociety);
                    }

                    if (lawSociety != null)
                    {
                        var org = new PartyOrgainizationDetail
                        {
                            Party = party,
                            Organization = lawSociety
                        };
                        Serilog.Log.Information($"Adding {user.GetUserId} to organization {lawSociety.Code}");
                        this.context.PartyOrgainizationDetails.Add(org);
                    }

                }
                else
                {
                    // check if from submitting agency (or later verified credentials )
                    var idp = user.GetIdentityProvider();

                    var submittingAgencies = this.GetSubmittingAgencies(this.context);

                    var agency = submittingAgencies.Find(agency => agency.IdpHint.Equals(idp, StringComparison.Ordinal));



                    if (agency != null)
                    {
                        var organization = this.GetOrganization(this.context, agency.IdpHint);

                        if (organization == null)
                        {
                            Serilog.Log.Warning($"Agency {agency.Name} [{agency.IdpHint}] is not in organization table - adding");

                            var added = await this.context.Organizations.AddAsync(new Organization
                            {
                                IdpHint = agency.IdpHint,
                                Name = agency.Name
                            });

                            if (added.State == EntityState.Added)
                            {
                                organization = added.Entity;
                            }
                            else
                            {
                                Serilog.Log.Error($"Failed to add organization {agency.Name} [{agency.IdpHint}]");
                            }


                        }

                        if (organization != null)
                        {
                            // check if user is already assigned to organization
                            var partyOrg = this.context.PartyOrgainizationDetails.Where(p => p.PartyId == party.Id && p.OrganizationCode == organization.Code).FirstOrDefault();

                            if (partyOrg != null)
                            {
                                Serilog.Log.Information($"User {party.Jpdid} is already assigned to agency {organization.Code}");

                            }
                            else
                            {
                                Serilog.Log.Information($"User {party.Jpdid} is from agency {agency.Name} - automatically assigning organization", user.GetUserId(), agency.Name);

                                var org = new PartyOrgainizationDetail
                                {
                                    Party = party,
                                    Organization = organization
                                };
                                this.context.PartyOrgainizationDetails.Add(org);
                            }
                        }
                    }
                }
            }




            await this.context.SaveChangesAsync();
            return party.Id;
        }

        public Organization? GetOrganization(PidpDbContext context, string idpHint)
        {
            // create an instance of the Query class
            var query = new Lookups.Index.Query();

            // create an instance of the QueryHandler class
            var handler = new Lookups.Index.QueryHandler(context);

            // execute the query and get the result
            var result = handler.HandleAsync(query);

            // get the SubmittingAgencies list from the result
            return result.Result.Organizations.FirstOrDefault(org => org.IdpHint.Equals(idpHint, StringComparison.Ordinal));
        }

        public List<SubmittingAgency> GetSubmittingAgencies(PidpDbContext context)
        {
            // create an instance of the Query class
            var query = new Lookups.Index.Query();

            // create an instance of the QueryHandler class
            var handler = new Lookups.Index.QueryHandler(context);

            // execute the query and get the result
            var result = handler.HandleAsync(query);

            // get the SubmittingAgencies list from the result
            return result.Result.SubmittingAgencies;
        }

        private async Task<UserTypeLookup> GetAndAddUserTypeLookup(string name, string description)
        {
            var userType = await this.context.UserTypeLookups.Where(t => t.Name.Equals(name)).FirstOrDefaultAsync();
            if (userType == null)
            {
                Serilog.Log.Information($"Adding new user type {name}");
                // should go to a service interface really
                this.context.UserTypeLookups.Add(
                    new UserTypeLookup
                    {
                        Name = name,
                        Description = description
                    });
                await this.context.SaveChangesAsync();
            }

            return userType;

        }

    }



}
