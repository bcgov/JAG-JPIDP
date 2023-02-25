namespace Pidp.Features.DigitalEvidenceCaseManagement.BackgroundServices;

using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Data;
using Pidp.Kafka.Interfaces;
using Pidp.Models;

public class DecomissionCaseAccessService : BackgroundService
{
    private readonly IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer;
    private readonly PidpConfiguration config;
    private readonly ILogger<DecomissionCaseAccessService> logger;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private PidpDbContext context;
    private readonly IClock clock;
    public DecomissionCaseAccessService(IKafkaProducer<string, SubAgencyDomainEvent> kafkaProducer, PidpConfiguration config, ILogger<DecomissionCaseAccessService> logger, IServiceScopeFactory serviceScopeFactory, IClock clock)
    {
        this.kafkaProducer = kafkaProducer;
        this.config = config;
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
        this.clock = clock;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogDebug($"{nameof(DecomissionCaseAccessService)} in starting");

        var period = TimeSpan.FromHours(this.config.BackGroundServices.DecomissionCaseAccessService.PeriodicTimer);
        stoppingToken.Register(() => this.logger.LogDebug("#1 DecomissionCaseAccessService background task is stopping."));
        using var timer = new PeriodicTimer(period);
        using var scope = this.serviceScopeFactory.CreateScope();

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            this.context = scope.ServiceProvider.GetRequiredService<PidpDbContext>();
            using var trx = this.context.Database.BeginTransaction();
            try
            {
                this.logger.LogDebug($"{nameof(DecomissionCaseAccessService)} background task is doing background work.");

                var expiredCaseAccessrequest = await this.GetExpiredCaseAccessRequests();
                if (expiredCaseAccessrequest.Any())
                {

                    this.context.SubmittingAgencyRequests.RemoveRange(expiredCaseAccessrequest);

                    await this.PublishSubAgencyAccessRequest(expiredCaseAccessrequest);

                    await this.context.SaveChangesAsync(stoppingToken);
                    await trx.CommitAsync(stoppingToken);
                }
            }
            catch (Exception)
            {
                await trx.RollbackAsync(stoppingToken);
                throw;
            }

        }

    }
    private async Task PublishSubAgencyAccessRequest(IEnumerable<SubmittingAgencyRequest> subAgencyRequests)
    {
        foreach (var caseAccessRequest in subAgencyRequests)
        {
            Serilog.Log.Logger.Information("Publishing Evidence Auto Decomisison Domain Event to topic {0} {1}", this.config.KafkaCluster.CaseDecomissionTopicName, caseAccessRequest.RequestId);
            await this.kafkaProducer.ProduceAsync(this.config.KafkaCluster.CaseDecomissionTopicName, $"{caseAccessRequest.RequestId}", new SubAgencyDomainEvent
            {
                RequestId = caseAccessRequest.RequestId,
                CaseNumber = caseAccessRequest.CaseNumber,
                PartyId = caseAccessRequest.PartyId,
                AgencyCode = caseAccessRequest.AgencyCode,
                CaseGroup = caseAccessRequest.CaseGroup,
                Username = caseAccessRequest.Party!.Jpdid,
                RequestedOn = caseAccessRequest.RequestedOn,
            });
        }
    }

    private async Task<IEnumerable<SubmittingAgencyRequest>> GetExpiredCaseAccessRequests()
    {
        return await this.context.SubmittingAgencyRequests
            .Where(request => request.RequestedOn < this.clock.GetCurrentInstant().Minus(Duration.FromDays(this.config.BackGroundServices.DecomissionCaseAccessService.GracePeriod)))
            .Include(party => party.Party)
            .ToListAsync();
    }
}
