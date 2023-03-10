namespace Pidp.Features.Parties;

using FluentValidation;
using NodaTime;
using System.Security.Claims;

using Pidp.Data;
using Pidp.Extensions;
using Pidp.Infrastructure.Auth;
using Pidp.Models;

public class Create
{
    public class Command : ICommand<int>
    {
        public Guid UserId { get; set; }
        public string? Jpdid { get; set; }
        public LocalDate? Birthdate { get; set; }
        public string? Gender { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IHttpContextAccessor accessor)
        {
            var user = accessor?.HttpContext?.User;

            this.RuleFor(x => x.UserId).NotEmpty().Equal(user.GetUserId());
            this.RuleFor(x => x.FirstName).NotEmpty().MatchesUserClaim(user, Claims.GivenName);
            this.RuleFor(x => x.LastName).NotEmpty().MatchesUserClaim(user, Claims.FamilyName);

            this.Include<AbstractValidator<Command>>(x => user.GetIdentityProvider() switch
            {
                ClaimValues.BCServicesCard => new BcscValidator(user),
                ClaimValues.Phsa => new PhsaValidator(),
                ClaimValues.Bcps => new BcpsValidator(user),
                ClaimValues.Idir => new IdirValidator(user),
                _ => throw new NotImplementedException("Given Identity Provider is not supported")
            });
        }

        private class BcscValidator : AbstractValidator<Command>
        {
            public BcscValidator(ClaimsPrincipal? user)
            {
                this.RuleFor(x => x.Jpdid).NotEmpty().MatchesUserClaim(user, Claims.PreferredUsername);
                this.RuleFor(x => x.Birthdate).NotEmpty().Equal(user?.GetBirthdate()).WithMessage($"Must match the \"birthdate\" Claim on the current User");
                this.RuleFor(x => x.Gender).NotEmpty().Equal(user?.GetGender()).WithMessage($"Must match the \"gender\" Claim on the current User");
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
    }

    public class CommandHandler : ICommandHandler<Command, int>
    {
        private readonly PidpDbContext context;

        public CommandHandler(PidpDbContext context) => this.context = context;

        public async Task<int> HandleAsync(Command command)
        {
            var party = new Party
            {
                UserId = command.UserId,
                Jpdid = command.Jpdid,
                Gender = command.Gender,
                Birthdate = command.Birthdate,
                FirstName = command.FirstName,
                LastName = command.LastName,
                Email = command.Email
            };

            this.context.Parties.Add(party);

            await this.context.SaveChangesAsync();

            return party.Id;
        }
    }
}
