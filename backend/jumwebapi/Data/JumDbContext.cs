namespace jumwebapi.Data;

using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using jumwebapi.Data.ef;
using jumwebapi.Features.UserChangeManagement.Data;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class JumDbContext : DbContext
{
    private readonly IClock clock;
    private readonly JumWebApiConfiguration configuration;

    public JumDbContext(DbContextOptions<JumDbContext> options, IClock clock, JumWebApiConfiguration configuration) : base(options)
    {

        this.clock = clock;
        this.configuration = configuration;
    }


    public DbSet<ParticipantMerge> ParticipantMerges { get; set; } = default!;
    public DbSet<IdempotentConsumers> IdempotentConsumer { get; set; } = default!;

    public DbSet<JustinUserChange> JustinUserChange { get; set; } = default!;


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

        // Adds Quartz.NET PostgreSQL schema to EntityFrameworkCore
        modelBuilder.AddQuartz(builder => builder.UsePostgreSql());

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JumDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(this.configuration.ConnectionStrings.JumDatabase, x => x.MigrationsHistoryTable(this.configuration.ConnectionStrings.EfHistoryTable, this.configuration.ConnectionStrings.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL")))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }

    /// <summary>
    /// Check if a message was already recorded as processed
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="consumer"></param>
    /// <returns></returns>
    public async Task<bool> HasBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumer.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);

    /// <summary>
    /// Record message as processed
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="consumer"></param>
    /// <returns></returns>
    public async Task AddIdempotentConsumer(string messageId, string consumer)
    {
        await this.IdempotentConsumer.AddAsync(new IdempotentConsumers
        {
            MessageId = messageId,
            Consumer = consumer,
            ConsumeDate = this.clock.GetCurrentInstant()
        });
        await this.SaveChangesAsync();
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

}
