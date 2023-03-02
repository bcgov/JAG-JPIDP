namespace Pidp.Features.DigitalEvidenceCaseManagement.Commands;

using System.Diagnostics;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Features.AccessRequests;
using Pidp.Kafka.Interfaces;
using Pidp.Models;
using Pidp.Models.Lookups;

public class DigitalEvidenceCaseCommand
{
    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public int CaseId { get; set; }

        public bool RemoveRequested { get; set; }
        public string AgencyFileNumber { get; set; } = string.Empty;

    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.CaseId).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
        }
    }

    public class Query : IQuery<Command>
    {
        public int CaseId { get; set; }
    }

    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly PidpDbContext context;
        private readonly IKafkaProducer<string, DigitalEvidenceCaseRequest> kafkaProducer;

        public CommandHandler(
          IClock clock,
          ILogger<CommandHandler> logger,
          PidpConfiguration config,
          PidpDbContext context,
          IKafkaProducer<string, DigitalEvidenceCaseRequest> kafkaProducer)
        {
            this.clock = clock;
            this.logger = logger;
            this.context = context;
            this.kafkaProducer = kafkaProducer;
            this.config = config;
        }


        public async Task<IDomainResult> HandleAsync(Command command)
        {

            using (var activity = new Activity("DigitalEvidence Case Request").Start())
            {
                Serilog.Log.Information("Handling case request");

                var party = await this.GetPidpUser(command);

                // validate from a valid submitting agency?

                // add database access request
                using var trx = this.context.Database.BeginTransaction();

                try
                {
                    Serilog.Log.Information("Case request received {0} {1}", command.PartyId, command.CaseId);

                    var caseRequest = await this.StoreCaseRequest(command);

                    // place message onto request topic
                    var submission = await this.SubmitToRequestTopic(command, party, caseRequest);


                    await this.context.SaveChangesAsync();
                    await trx.CommitAsync();
                }
                catch (Exception ex)
                {
                    this.logger.LogDigitalEvidenceAccessTrxFailed(ex.Message.ToString());
                    await trx.RollbackAsync();
                    return DomainResult.Failed();
                }

                return DomainResult.Success();
            }
        }

        /// <summary>
        /// Submit the request message to the 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="party"></param>
        /// <param name="caseRequest"></param>
        /// <returns></returns>
        private async Task<IDomainResult> SubmitToRequestTopic(Command command, PartyDto party, DigitalEvidenceCase caseRequest)
        {
            Serilog.Log.Information("Case request submitting to topic {0} {1}", command.PartyId, command.CaseId);


            return DomainResult.Success();

        }

        private async Task<DigitalEvidenceCase> StoreCaseRequest(Command command)
        {

            var caseRequest = new DigitalEvidenceCase
            {
                PartyId = command.PartyId,
                Status = AccessRequestStatus.Pending,
                CaseId = command.CaseId,
                AgencyFileNumber = command.AgencyFileNumber,
                AccessTypeCode = AccessTypeCode.DigitalEvidenceCaseManagement,
                RequestedOn = this.clock.GetCurrentInstant(),
            };
            this.context.DigitalEvidenceCases.Add(caseRequest);

            await this.context.SaveChangesAsync();
            return caseRequest;
        }
        private async Task<PartyDto> GetPidpUser(Command command)
        {
            return await this.context.Parties
                .Where(party => party.Id == command.PartyId)
                .Select(party => new PartyDto
                {
                    AlreadyEnroled = party.AccessRequests.Any(request => request.AccessTypeCode == AccessTypeCode.DigitalEvidence),
                    Cpn = party.Cpn,
                    Jpdid = party.Jpdid,
                    UserId = party.UserId,
                    Email = party.Email,
                    FirstName = party.FirstName,
                    LastName = party.LastName,
                    Phone = party.Phone
                })
                .SingleAsync();
        }

    }
}
