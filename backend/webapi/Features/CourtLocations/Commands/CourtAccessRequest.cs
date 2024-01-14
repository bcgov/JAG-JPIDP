namespace Pidp.Features.CourtLocations.Commands;

using System;
using DomainResults.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Features.AccessRequests;
using Pidp.Features.DigitalEvidenceCaseManagement.Commands;
using Pidp.Models;
using Pidp.Models.Lookups;
using Prometheus;

public class CourtAccessRequest
{
    private readonly TimeZoneInfo timeZone;


    public class Command : ICommand<IDomainResult>
    {
        public int PartyId { get; set; }
        public CourtLocation CourtLocation { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public int TimeZoneOffset { get; set; }


    }
    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.CourtLocation).NotEmpty();
            this.RuleFor(x => x.PartyId).NotEmpty();
            this.RuleFor(x => x.PartyId).GreaterThan(0);
            this.RuleFor(x => x.ValidFrom).NotNull();
            this.RuleFor(x => x.ValidUntil).NotNull();

        }
    }
    public class CommandHandler : ICommandHandler<Command, IDomainResult>
    {
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly PidpConfiguration config;
        private readonly DateTime utcDateTime;
        private readonly ICourtAccessService courtAccessService;
        private readonly PidpDbContext context;
        private static readonly Histogram CourtLocationRequestDuration = Metrics
            .CreateHistogram("pidp_court_location_request_duration", "Histogram of court location request call durations.");

        public CommandHandler(IClock clock, ILogger<CommandHandler> logger, PidpConfiguration config, PidpDbContext context, ICourtAccessService courtAccessService)
        {
            this.clock = clock;
            this.logger = logger;
            this.config = config;
            this.context = context;
            this.courtAccessService = courtAccessService;
        }
        public DateTime UniversalTime => DateTime.Now.ToUniversalTime();


        public async Task<IDomainResult> HandleAsync(Command command)
        {
            using (CourtLocationRequestDuration.NewTimer())
            {
                command.ValidFrom = TimeZoneInfo.ConvertTimeToUtc(command.ValidFrom);
                command.ValidUntil.Date.AddDays(1);

                var dto = await this.GetPidpUser(command);

                if (!dto.AlreadyEnroled
                    || dto.Email == null) //user must be already enrolled i.e access to DEMS
                {
                    this.logger.LogUserNotEnroled(dto.Jpdid); //throw dems user not enrolled error
                    return DomainResult.Failed();
                }

                using var trx = this.context.Database.BeginTransaction();

                try
                {
                    var location = this.context.CourtLocations.Where(loc => loc.Code == command.CourtLocation.Code).FirstOrDefault();
                    CourtLocationAccessRequest courtLocationRequest = null;

                    if (location != null)
                    {
                        var subtractHours = command.TimeZoneOffset * -1;
                        var today = this.UniversalTime.AddHours(subtractHours);

                        // check if this is already requested
                        var existingRequests = this.context.CourtLocationAccessRequests.Where(clar => clar.PartyId == command.PartyId && clar.CourtLocation == location).ToList();
                        var existingRequestId = -1;
                        // see if existing request falls within new request timeframe
                        foreach (var req in existingRequests)
                        {
                            if (req.RequestStatus is CourtLocationAccessStatus.Error or CourtLocationAccessStatus.Deleted)
                            {
                                // ignore errored or deleted so they can be re-added
                                continue;
                            }

                            if (command.ValidFrom == req.ValidFrom && command.ValidUntil == req.ValidUntil)
                            {
                                Serilog.Log.Information($"Duplicate request - {req.RequestId} ignoring");
                                return DomainResult.Failed("Requested location and duration already present");

                            }
                            else if ((command.ValidFrom <= req.ValidUntil && command.ValidUntil >= req.ValidFrom) || (command.ValidFrom <= req.ValidUntil && command.ValidUntil >= req.ValidUntil))
                            {
                                Serilog.Log.Information($"Existing overlapping request - updating existing {req.RequestId}");
                                courtLocationRequest = req;
                                if (req.ValidFrom >= today && command.ValidFrom <= today)
                                {
                                    Serilog.Log.Information($"Update request is for access today {req.RequestId}");
                                }

                                // update existing request
                                req.ValidFrom = command.ValidFrom;
                                req.ValidUntil = command.ValidUntil;
                                req.Modified = this.clock.GetCurrentInstant();
                                Serilog.Log.Information($"Updated {req.RequestId} to {command.ValidFrom}-{command.ValidUntil}");

                            }
                        }

                        // if request is for today then we'll want to send an event immediately
                        var newRequest = false;
                        if (courtLocationRequest != null)
                        {
                            Serilog.Log.Information($"Updating request {courtLocationRequest.RequestId}");
                            // request has been moved to today from a future date
                            if (command.ValidFrom.DayOfYear == today.DayOfYear && courtLocationRequest.RequestStatus == CourtLocationAccessStatus.SubmittedFuture)
                            {
                                courtLocationRequest.RequestStatus = CourtLocationAccessStatus.Submitted;
                                newRequest = true;
                            }
                        }
                        else
                        {
                            newRequest = true;
                            courtLocationRequest = await this.courtAccessService.CreateCourtLocationRequest(command, location);
                            var duration = courtLocationRequest.ValidUntil - courtLocationRequest.ValidFrom;

                            var daysBetween = (int)duration.TotalDays;
                            Serilog.Log.Information($"Added request for location {location.Code} to party {command.PartyId} for {daysBetween} days");

                        }



                        if (command.ValidFrom.DayOfYear == today.DayOfYear && newRequest)
                        {
                            Serilog.Log.Information($"Request {courtLocationRequest.RequestId} is for today {today.ToLocalTime().DayOfYear} - adding to event topic");
                            var response = this.courtAccessService.CreateAddCourtAccessDomainEvent(courtLocationRequest);
                        }
                        else
                        {
                            Serilog.Log.Information($"Request {courtLocationRequest.RequestId} is for {courtLocationRequest.ValidFrom.DayOfYear} - today is  {today.ToLocalTime().DayOfYear} - marking future");

                        }

                        await this.context.SaveChangesAsync();
                        await trx.CommitAsync();
                    }
                    else
                    {
                        this.logger.LogInvalidCourtLocationRequest(dto.Jpdid, command.CourtLocation.Code, "Unknown location code");
                        await trx.RollbackAsync();

                    }



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

        private async Task<PartyDto> GetPidpUser(Command command)
        {
            return await this.context.Parties
                .Where(party => party.Id == command.PartyId)
                .Select(party => new PartyDto
                {
                    AlreadyEnroled = party.AccessRequests.Any(request => request.AccessTypeCode == AccessTypeCode.DigitalEvidence || request.AccessTypeCode == AccessTypeCode.DigitalEvidenceDefence),
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

public static partial class CourtLocationAccessLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Request for court location access denied for party {partyId} and location {locationCode}")]
    public static partial void LogCourtLocationAccessRequestDenied(this ILogger logger, int partyId, string locationCode);
    [LoggerMessage(2, LogLevel.Warning, "Court Location Access Request denied due to user {username} is not enrolled to DEMS.")]
    public static partial void LogUserNotEnrolled(this ILogger logger, string? username);
    [LoggerMessage(3, LogLevel.Error, "Invalid court location request for {username} and court {locationCode} [{reason}]")]
    public static partial void LogInvalidCourtLocationRequest(this ILogger logger, string? username, string locationCode, string reason);
}
