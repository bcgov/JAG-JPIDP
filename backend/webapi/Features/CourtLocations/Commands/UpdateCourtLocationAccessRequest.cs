namespace Pidp.Features.CourtLocations.Commands;

using DomainResults.Common;
using FluentValidation;
using NodaTime;

public class UpdateCourtLocationAccessRequest
{



    public class Command : ICommand<IDomainResult>
    {
        public int CourtLocationRequestId { get; set; }
        public Instant ValidFrom { get; set; }
        public Instant ValidTo { get; set; }
        public bool RemoveRequest { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.CourtLocationRequestId).NotEmpty();
            this.RuleFor(x => x.CourtLocationRequestId).GreaterThan(0);

        }
    }
}
