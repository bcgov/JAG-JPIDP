namespace Pidp.Features.DigitalEvidenceCaseManagement.Commands;

using System.Text.Json.Serialization;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Kafka.Interfaces;
using Pidp.Models;

public class RemoveCaseAccess
{
    public sealed record Command(int RequestId) : ICommand;
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.RequestId).NotEmpty();
            this.RuleFor(x => x.RequestId).GreaterThan(0);

        }
    }
    public class CommandHandler : ICommandHandler<Command>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer;

        public CommandHandler(IClock clock, ILogger<CommandHandler> logger, PidpConfiguration config, PidpDbContext context, IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer)
        {
            this.clock = clock;
            this.logger = logger;
            this.config = config;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
        }
        public async Task HandleAsync(Command command)
        {

            var agencyRequest = await this.context.SubmittingAgencyRequests
                .Where(request => request.RequestId == command.RequestId)
                .SingleAsync();

            if (agencyRequest == null)
            {
                return;
            }
            this.context.SubmittingAgencyRequests.Remove(agencyRequest);
            await this.context.SaveChangesAsync();
        }
    }
}
