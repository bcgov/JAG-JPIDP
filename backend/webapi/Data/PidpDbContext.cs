namespace Pidp.Data;

using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using Pidp.Models;
using Pidp.Models.Lookups;
using Pidp.Models.OutBoxEvent;
using Pidp.Models.ProcessFlow;
using Pidp.Models.UserInfo;

public class PidpDbContext : DbContext
{
    private readonly IClock clock;
    private readonly PidpConfiguration configuration;

    public PidpDbContext(DbContextOptions<PidpDbContext> options, IClock clock, PidpConfiguration configuration) : base(options)
    {
        this.clock = clock;
        this.configuration = configuration;
    }

    public DbSet<AccessRequest> AccessRequests { get; set; } = default!;
    public DbSet<ClientLog> ClientLogs { get; set; } = default!;
    public DbSet<EmailLog> EmailLogs { get; set; } = default!;

    public DbSet<EndorsementRelationship> EndorsementRelationships { get; set; } = default!;
    public DbSet<EndorsementRequest> EndorsementRequests { get; set; } = default!;
    public DbSet<Endorsement> Endorsements { get; set; } = default!;
    public DbSet<Facility> Facilities { get; set; } = default!;
    public DbSet<FutureUserChangeEvent> FutureUserChangeEvents { get; set; } = default!;

    public DbSet<HcimAccountTransfer> HcimAccountTransfers { get; set; } = default!;
    public DbSet<HcimEnrolment> HcimEnrolments { get; set; } = default!;
    public DbSet<DigitalEvidence> DigitalEvidences { get; set; } = default!;
    public DbSet<DigitalEvidenceDisclosure> DigitalEvidenceDisclosures { get; set; } = default!;
    public DbSet<DigitalEvidenceDefence> DigitalEvidenceDefences { get; set; } = default!;
    public DbSet<JustinAppAccessRequest> JAMRequests { get; set; } = default!;

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
    public DbSet<PartyUserType> PartyUserTypes { get; set; } = default!;
    public DbSet<UserTypeLookup> UserTypeLookups { get; set; } = default!;

    public DbSet<PlrRecord> PlrRecords { get; set; } = default!;


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

        modelBuilder.HasDefaultSchema(this.configuration.ConnectionStrings.Schema);

        modelBuilder.Entity<DigitalEvidence>().Property(x => x.AssignedRegions).
            HasConversion(
                          v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                v => JsonConvert.DeserializeObject<List<AssignedRegion>>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })

            );

        modelBuilder.Entity<IdempotentConsumer>()
            .ToTable("IdempotentConsumers")
            .HasKey(x => new { x.MessageId, x.Consumer });

        modelBuilder.Entity<ExportedEvent>()
             .ToTable("OutBoxedExportedEvent");
        //.Property(x => x.JsonEventPayload).HasColumnName("EventPayload");


        // add court locations

        modelBuilder.Entity<CourtLocation>().HasData(
            new CourtLocation { Code = "3561", Name = "Abbotsford Law Courts", Active = true, Staffed = true },
            new CourtLocation { Code = "2011", Name = "North Vancouver Provincial Court", Active = true, Staffed = true },
            new CourtLocation { Code = "3531", Name = "Port Coquitlam Law Courts", Active = true, Staffed = true },
            new CourtLocation { Code = "2025", Name = "Richmond Provincial Courts", Active = true, Staffed = true },
            new CourtLocation { Code = "3585-A", Name = "Surrey Provincial Court-Adult", Active = true, Staffed = true },
            new CourtLocation { Code = "3585-Y", Name = "Surrey Provincial Court-IPV and Youth", Active = true, Staffed = true },
            new CourtLocation { Code = "2042", Name = "Downtown Community Court", Active = true, Staffed = true },
            new CourtLocation { Code = "2040", Name = "Vancouver Provincial Court ", Active = true, Staffed = true },
            new CourtLocation { Code = "2045", Name = "Robson Square Provincial Court-Youth", Active = true, Staffed = true }
        );


        // Adds Quartz.NET PostgreSQL schema to EntityFrameworkCore
        modelBuilder.AddQuartz(builder => builder.UsePostgreSql());

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
        optionsBuilder.UseNpgsql(this.configuration.ConnectionStrings.PidpDatabase, x => x.MigrationsHistoryTable(this.configuration.ConnectionStrings.EfHistoryTable, this.configuration.ConnectionStrings.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL")))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }
    public async Task AddIdempotentConsumer(string messageId, string consumer)
    {
        await this.IdempotentConsumers.AddAsync(new IdempotentConsumer
        {
            MessageId = messageId,
            Consumer = consumer
        });
        await this.SaveChangesAsync();
    }

    public async Task<bool> HasMessageBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);

}
