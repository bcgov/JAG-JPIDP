namespace ApprovalFlow.Data;

using ApprovalFlow.Data.Approval;
using ApprovalFlow.Models;
using DIAM.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

public class ApprovalFlowDataStoreDbContext(DbContextOptions<ApprovalFlowDataStoreDbContext> options, IClock clock, ApprovalFlowConfiguration config) : DbContext(options)
{
    private readonly IClock clock = clock;

    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = default!;
    public DbSet<Request> Requests { get; set; } = default!;
    public DbSet<ApprovalHistory> ApprovalHistories { get; set; } = default!;
    public DbSet<PersonalIdentity> PersonalIdentities { get; set; } = default!;



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
        modelBuilder.HasDefaultSchema(config.ConnectionStrings.Schema);
        base.OnModelCreating(modelBuilder);


        modelBuilder
        .Entity<Request>()
        .Property(d => d.ApprovalType)
        .HasConversion(new EnumToStringConverter<ApprovalType>());

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApprovalFlowDataStoreDbContext).Assembly);
    }

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

    public async Task IdempotentConsumer(string messageId, string consumer, Instant consumeDate)
    {
        await this.IdempotentConsumers.AddAsync(new IdempotentConsumer
        {
            MessageId = messageId,
            Consumer = consumer,
            ConsumeDate = consumeDate
        });
        await this.SaveChangesAsync();
    }

    public async Task<bool> HasBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(config.ConnectionStrings.ApprovalFlowDataStore, x => x.MigrationsHistoryTable(config.ConnectionStrings.EfHistoryTable, config.ConnectionStrings.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL")))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }
}
