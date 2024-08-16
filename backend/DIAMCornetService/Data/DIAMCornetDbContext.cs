namespace DIAMCornetService.Data;

using DIAMCornetService.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

/// <summary>
/// Database context for DIAM Cornet
/// </summary>
/// <param name="options"></param>
/// <param name="clock"></param>
/// <param name="configuration"></param>
public class DIAMCornetDbContext(DbContextOptions<DIAMCornetDbContext> options, IClock clock, DIAMCornetServiceConfiguration configuration) : DbContext(options)
{
    public DbSet<IncomingMessage> IncomingMessages { get; set; } = default!;
    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;

    // use schema
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(configuration.ConnectionStrings.Schema);
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DIAMCornetDbContext).Assembly);
    }

    // EF settings
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(configuration.ConnectionStrings.DIAMCornetDatabase, x => x.MigrationsHistoryTable(configuration.ConnectionStrings.EfHistoryTable, configuration.ConnectionStrings.EfHistorySchema));

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
            Consumer = consumer,
            ConsumeDate = clock.GetCurrentInstant()
        });
        await this.SaveChangesAsync();
    }

    public async Task<bool> HasMessageBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);

}
