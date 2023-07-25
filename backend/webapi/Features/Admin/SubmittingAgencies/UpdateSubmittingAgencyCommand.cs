
using AutoMapper;
using DomainResults.Common;
using FluentValidation;
using NodaTime;
using Pidp;
using Pidp.Data;
using Pidp.Features;
using Pidp.Features.Organization.OrgUnitService;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Models;
using Pidp.Models.Lookups;
using Prometheus;

public class UpdateSubmittingAgencyCommand
{
    public class Command : ICommand<IDomainResult<SubmittingAgencyModel>>
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public Instant? ClientCertExpiry { get; set; }
        public string? IdpHint { get; set; }
        public int? LevelOfAssurance { get; set; }


    }


    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(command => command.Code).NotNull();
            this.RuleFor(command => command.Name).NotNull();

        }
    }


    public class CommandHandler : ICommandHandler<Command, IDomainResult<SubmittingAgencyModel>>
    {
        private readonly IClock clock;
        private readonly IMapper mapper;
        private readonly IKeycloakAdministrationClient keycloakClient;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IOrgUnitService orgUnitService;

        public CommandHandler(
            IClock clock,
            IKeycloakAdministrationClient keycloakClient,
            ILogger<CommandHandler> logger,
            IMapper mapper,
            PidpConfiguration config,
            IOrgUnitService orgUnitService,
            PidpDbContext context)
        {
            this.clock = clock;
            this.keycloakClient = keycloakClient;
            this.logger = logger;
            this.context = context;
            this.mapper =  mapper;
            this.config = config;
            this.orgUnitService = orgUnitService;
        }

        public async Task<IDomainResult<SubmittingAgencyModel>> HandleAsync(Command command)
        {
            Serilog.Log.Information($"Update event for {command.Name}");

            var agency = this.context.SubmittingAgencies.Where(agency => agency.Code == command.Code).FirstOrDefault();

            var txn = this.context.Database.BeginTransaction();
            if (agency != null)
            {
                agency.Name = command.Name;
                agency.LevelOfAssurance = command.LevelOfAssurance;
                agency.ClientCertExpiry = command.ClientCertExpiry;
                agency.IdpHint = command.IdpHint;

                // check if this idp hint exists in keycloak
                var realm = await this.keycloakClient.GetRealm(command.IdpHint);

                if (realm != null)
                {
                    Serilog.Log.Information($"Found matching keycloak realm for {command.Name} {command.IdpHint}");

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

            }
            else
            {
                // create a new record

            }

            var responseModel = this.mapper.Map<SubmittingAgencyModel>(agency);
            await txn.CommitAsync();

            return DomainResult.Success(responseModel);
        }


    }
}
