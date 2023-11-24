namespace Pidp.Data;

using Microsoft.EntityFrameworkCore;
using NodaTime;
using Pidp.Models;
using Pidp.Models.Lookups;
using Pidp.Models.OutBoxEvent;
using Pidp.Models.ProcessFlow;
using Pidp.Models.UserInfo;

public class PidpDbContext : DbContext
{
    private readonly IClock clock;

    public PidpDbContext(DbContextOptions<PidpDbContext> options, IClock clock) : base(options) => this.clock = clock;

    public DbSet<AccessRequest> AccessRequests { get; set; } = default!;
    public DbSet<ClientLog> ClientLogs { get; set; } = default!;
    public DbSet<EmailLog> EmailLogs { get; set; } = default!;

    public DbSet<EndorsementRelationship> EndorsementRelationships { get; set; } = default!;
    public DbSet<EndorsementRequest> EndorsementRequests { get; set; } = default!;
    public DbSet<Endorsement> Endorsements { get; set; } = default!;
    public DbSet<Facility> Facilities { get; set; } = default!;
    public DbSet<FutureUserChangeEvent> FutureUserChangeEvents { get; set; } = default!;

    public DbSet<Models.HcimAccountTransfer> HcimAccountTransfers { get; set; } = default!;
    public DbSet<Models.HcimEnrolment> HcimEnrolments { get; set; } = default!;
    public DbSet<Models.DigitalEvidence> DigitalEvidences { get; set; } = default!;
    public DbSet<DigitalEvidenceDisclosure> DigitalEvidenceDisclosures { get; set; } = default!;
    public DbSet<Models.DigitalEvidenceDefence> DigitalEvidenceDefences { get; set; } = default!;

    public DbSet<PartyLicenceDeclaration> PartyLicenceDeclarations { get; set; } = default!;
    public DbSet<Party> Parties { get; set; } = default!;
    public DbSet<ExportedEvent> ExportedEvents { get; set; } = default!;
    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<PartyAccessAdministrator> PartyAccessAdministrators { get; set; } = default!;
    public DbSet<PartyOrgainizationDetail> PartyOrgainizationDetails { get; set; } = default!;
    public DbSet<JusticeSectorDetail> JusticeSectorDetails { get; set; } = default!;
    public DbSet<CorrectionServiceDetail> CorrectionServiceDetails { get; set; } = default!;
    public DbSet<SubmittingAgencyRequest> SubmittingAgencyRequests { get; set; } = default!;
    public DbSet<AgencyRequestAttachment> AgencyRequestAttachments { get; set; } = default!;
    public DbSet<CourtLocationAccessRequest> CourtLocationAccessRequests { get; set; } = default!;
    public DbSet<CourtLocation> CourtLocations { get; set; } = default!;
    public DbSet<Organization> Organizations { get; set; } = default!;
    public DbSet<SubmittingAgency> SubmittingAgencies { get; set; } = default!;

    public DbSet<UserAccountChange> UserAccountChanges { get; set; } = default!;
    public DbSet<ProcessFlow> ProcessFlows { get; set; } = default!;
    public DbSet<DomainEventProcessStatus> DomainEventProcessStatus { get; set; } = default!;
    public DbSet<DeferredEvent> DeferredEvents { get; set; } = default!;
    public DbSet<PublicUserValidation> PublicUserValidations { get; set; } = default!;
    public DbSet<DigitalEvidencePublicDisclosure> DigitalEvidencePublicDisclosures { get; set; } = default!;

    public override int SaveChanges()
    {
        this.ApplyAudits();

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyAudits();

        return await base.SaveChangesAsync(cancellationToken);
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdempotentConsumer>()
            .ToTable("IdempotentConsumers")
            .HasKey(x => new { x.MessageId, x.Consumer });

        modelBuilder.Entity<ExportedEvent>()
             .ToTable("OutBoxedExportedEvent");
        //.Property(x => x.JsonEventPayload).HasColumnName("EventPayload");

        modelBuilder.Entity<ExportedEvent>()
            .ToTable("OutBoxedExportedEvent");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PidpDbContext).Assembly);



    }

    public async Task IdempotentConsumer(string messageId, string consumer)
    {
        await this.IdempotentConsumers.AddAsync(new IdempotentConsumer
        {
            MessageId = messageId,
            Consumer = consumer
        });
        await this.SaveChangesAsync();
    }

    public async Task<bool> HasBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);
    private void ApplyAudits()
    {
        this.ChangeTracker.DetectChanges();
        var updated = this.ChangeTracker.Entries()
            .Where(x => x.Entity is BaseAuditable
                && (x.State == EntityState.Added || x.State == EntityState.Modified));

        var currentInstant = this.clock.GetCurrentInstant();

        foreach (var entry in updated)
        {
            entry.CurrentValues[nameof(BaseAuditable.Modified)] = currentInstant;

            if (entry.State == EntityState.Added)
            {
                entry.CurrentValues[nameof(BaseAuditable.Created)] = currentInstant;
            }
            else
            {
                entry.Property(nameof(BaseAuditable.Created)).IsModified = false;
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL")))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }
}
