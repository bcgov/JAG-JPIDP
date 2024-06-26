using Microsoft.EntityFrameworkCore;
using NodaTime;
using NotificationService.Features.DataGeneration.Model;
using NotificationService.NotificationEvents.UserProvisioning.Models;

namespace NotificationService.Data;
public class NotificationDbContext : DbContext
{
    private readonly IClock clock;
    private readonly NotificationServiceConfiguration configuration;

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options, IClock clock, NotificationServiceConfiguration configuration) : base(options)
    {
        this.clock = clock;
        this.configuration = configuration;
    }

    public DbSet<EmailLog> EmailLogs { get; set; } = default!;
    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<NotificationAckModel> Notifications { get; set; } = default!;
    public DbSet<TemplateGenerator> TemplateGenerators { get; set; } = default!;

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

        modelBuilder.Entity<IdempotentConsumer>()
            .ToTable("IdempotentConsumers")
            .HasKey(x => new { x.MessageId, x.Consumer });

        modelBuilder.Entity<NotificationAckModel>()
            .ToTable("Notifications")
            .HasKey(x => new { x.NotificationId, x.EmailAddress });

        modelBuilder.Entity<TemplateGenerator>()
            .ToTable("TemplateGenerator")
            .HasKey(x => new { x.TemplateName, x.Type });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
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

    public async Task IdempotentConsumer(string messageId, string consumer)
    {
        await IdempotentConsumers.AddAsync(new IdempotentConsumer
        {
            MessageId = messageId,
            Consumer = consumer
        });
        await SaveChangesAsync();
    }
    public async Task<bool> HasBeenProcessed(string messageId) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(this.configuration.ConnectionStrings.NotificationDatabase, x => x.MigrationsHistoryTable(this.configuration.ConnectionStrings.EfHistoryTable, this.configuration.ConnectionStrings.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL")))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }
}
