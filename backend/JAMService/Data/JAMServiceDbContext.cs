namespace JAMService.Data;

using Common.Models;
using Microsoft.EntityFrameworkCore;

public class JAMServiceDbContext(DbContextOptions<JAMServiceDbContext> options
       , IConfiguration configuration) : DbContext(options)
{

    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;

    public async Task<bool> HasBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        var schema = configuration.GetValue<string>("DatabaseConnectionInfo:Schema");

        modelBuilder.HasDefaultSchema(schema);
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JAMServiceDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL"), StringComparison.Ordinal))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }

}
