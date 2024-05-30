namespace edt.disclosure.Data;

using edt.disclosure.ServiceEvents.Models;
using edt.disclosure.ServiceEvents.UserAccountCreation.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class DisclosureDataStoreDbContext : DbContext
{
    private readonly IClock clock;
    private readonly EdtDisclosureServiceConfiguration configuration;

    public DisclosureDataStoreDbContext(DbContextOptions<DisclosureDataStoreDbContext> options, IClock clock, EdtDisclosureServiceConfiguration configuration) : base(options)
    {
        this.clock = clock;
        this.configuration = configuration;
    }

    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<CourtLocationRequest> CourtLocationRequests { get; set; } = default!;


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
        modelBuilder.HasDefaultSchema(this.configuration.ConnectionStrings.Schema);
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DisclosureDataStoreDbContext).Assembly);
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
        optionsBuilder.UseNpgsql(this.configuration.ConnectionStrings.DisclosureDataStore, x => x.MigrationsHistoryTable(this.configuration.ConnectionStrings.EfHistoryTable, this.configuration.ConnectionStrings.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL")))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }
}
