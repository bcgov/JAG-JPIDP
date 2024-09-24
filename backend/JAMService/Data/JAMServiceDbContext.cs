namespace JAMService.Data;

using Common.Models;
using JAMService.Entities;
using Microsoft.EntityFrameworkCore;

public class JAMServiceDbContext(DbContextOptions<JAMServiceDbContext> options
       , JAMServiceConfiguration configuration) : DbContext(options)
{

    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<AppRequest> AppRequests { get; set; } = default!;


    public async Task<bool> HasBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        var schema = configuration.DatabaseConnectionInfo.Schema;

        modelBuilder.HasDefaultSchema(schema);
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JAMServiceDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

        optionsBuilder.UseNpgsql(configuration.DatabaseConnectionInfo.JAMServiceConnection, x => x.MigrationsHistoryTable(configuration.DatabaseConnectionInfo.EfHistoryTable, configuration.DatabaseConnectionInfo.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL"), StringComparison.Ordinal))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }

}
