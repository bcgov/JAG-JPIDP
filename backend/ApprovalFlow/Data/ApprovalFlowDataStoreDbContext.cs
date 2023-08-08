namespace ApprovalFlow.Data;

using ApprovalFlow.Features.Approval;
using ApprovalFlow.Models;
using DIAM.Common.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class ApprovalFlowDataStoreDbContext : DbContext
{
    private readonly IClock clock;

    public ApprovalFlowDataStoreDbContext(DbContextOptions<ApprovalFlowDataStoreDbContext> options, IClock clock) : base(options) => this.clock = clock;
    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = default!;



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
        modelBuilder.HasDefaultSchema("approval");
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApprovalRequest>()
        .HasMany(e => e.History)
        .WithOne(e => e.ApprovalRequest)
        .HasForeignKey(e => e.ApprovalRequestId)
        .HasPrincipalKey(e => e.Id);


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


}
