namespace Pidp.Features.EndorsementRequests;

using System.ComponentModel.DataAnnotations;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;

using Pidp.Data;

public class Decline
{
    public class Command : ICommand<IDomainResult>
    {
        [Required]
        public int PartyId { get; set; }
        [Required]
        public int EndorsementRequestId { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.EndorsementRequestId).GreaterThan(0);
        }
    }

    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly PidpDbContext context;

        public CommandHandler(IClock clock, PidpDbContext context)
        {
            this.clock = clock;
            this.context = context;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            var endorsementRequest = await this.context.EndorsementRequests
                .SingleOrDefaultAsync(request => request.Id == command.EndorsementRequestId);

            if (endorsementRequest == null)
            {
                return DomainResult.NotFound();
            }

            var result = endorsementRequest.Handle(command, this.clock);
            if (!result.IsSuccess)
            {
                return result;
            }

            await this.context.SaveChangesAsync();

            return DomainResult.Success();
        }
    }
}
