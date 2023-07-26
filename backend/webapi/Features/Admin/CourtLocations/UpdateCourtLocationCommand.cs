namespace Pidp.Features.Admin.CourtLocations;

using AutoMapper;
using DomainResults.Common;
using FluentValidation;
using NodaTime;
using Pidp.Data;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Models;
using Pidp.Models.Lookups;

public class UpdateCourtLocationCommand
{
    public class Command : ICommand<IDomainResult<CourtLocation>>
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public bool Active { get; set; }
        public bool Delete { get; set; }

    }


    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(command => command.Code).NotNull();
            this.RuleFor(command => command.Name).NotNull();

        }
    }

    public class CommandHandler : ICommandHandler<Command, IDomainResult<CourtLocation>>
    {
        private readonly IClock clock;
        private readonly IMapper mapper;
        private readonly ILogger logger;
        private readonly PidpDbContext context;

        public CommandHandler(
            IClock clock,
            ILogger<CommandHandler> logger,
            IMapper mapper,
            PidpConfiguration config,
            IOrgUnitService orgUnitService,
            PidpDbContext context)
        {
            this.clock = clock;
            this.logger = logger;
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<IDomainResult<CourtLocation>> HandleAsync(Command command)
        {
            Serilog.Log.Information($"Update event for court location {command.Name}");
            
            var court = this.context.CourtLocations.Where(loc => loc.Code == command.Code).FirstOrDefault();
            var txn = this.context.Database.BeginTransaction();
            try
            {
                if (court != null)
                {
                    court.Name = command.Name;
                    court.Active = command.Active;
                }

                var response = await this.context.SaveChangesAsync();
                if (response > 0)
                {
                    Serilog.Log.Information($"Update successful for {command.Name}");
                }
                else
                {
                    Serilog.Log.Warning($"Update failed for {command.Name}");

                }

                await txn.CommitAsync();
            } catch (Exception ex)
            {
                Serilog.Log.Error($"Failed to update court location {command.Name} [{ex.Message}]");

                await txn.RollbackAsync();
            }

            return DomainResult.Success(court);
        }
    }
}
