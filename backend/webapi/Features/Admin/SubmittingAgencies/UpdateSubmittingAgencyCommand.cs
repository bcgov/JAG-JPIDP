
using DomainResults.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Pidp.Features;
using Pidp.Models.Lookups;

public class UpdateSubmittingAgencyCommand
{
    public class Command : ICommand<IDomainResult>
    {
        public SubmittingAgency? UpdateEvent { get; set; }
    }


    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {

        }
    }


    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        public async Task<IDomainResult> HandleAsync(Command command)
        {
            Serilog.Log.Information($"Update event for {command.UpdateEvent.Name}");
            return DomainResult.Success();
        }


    }
}
