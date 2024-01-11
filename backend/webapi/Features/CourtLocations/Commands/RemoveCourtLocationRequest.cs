namespace Pidp.Features.CourtLocations.Commands;

using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Prometheus;

public class RemoveCourtLocationRequest : ICommand<DomainResult>
{
    public sealed record Command(int RequestId, int PartyId) : ICommand<IDomainResult>;
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.RequestId).NotEmpty();
            this.RuleFor(x => x.RequestId).GreaterThan(0);
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.PartyId).NotEmpty();
        }
    }

    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly ICourtAccessService courtAccessService;
        private readonly PidpDbContext context;
        private static readonly Histogram CourtLocationRemovalRequestDuration = Metrics
            .CreateHistogram("pidp_court_location_delete_request_duration", "Histogram of court location delete request call durations.");

        public CommandHandler(IClock clock, ILogger<CommandHandler> logger, PidpConfiguration config, PidpDbContext context, ICourtAccessService courtAccessService)
        {
            this.clock = clock;
            this.logger = logger;
            this.config = config;
            this.context = context;
            this.courtAccessService = courtAccessService;
        }

        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using (CourtLocationRemovalRequestDuration.NewTimer())
            {
                try
                {
                    Serilog.Log.Information($"Deleting access to {command.RequestId}");

                    var accessRequest = this.context.CourtLocationAccessRequests.Include(req => req.Party).AsSplitQuery().Where(req => req.RequestId == command.RequestId).FirstOrDefault();


                    if (accessRequest != null)
                    {
                        await this.courtAccessService.DeleteAccessRequest(accessRequest);
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error($"Failed to set request {command.RequestId} to deleted");
                    return DomainResult.Failed(ex.Message);
                }
            }

            return DomainResult.Success();
        }

    }

}
