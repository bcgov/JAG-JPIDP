namespace JAMService.Data;

using Common.Models;
using JAMService.Entities;
using Microsoft.EntityFrameworkCore;

public class JAMServiceDbContext(DbContextOptions<JAMServiceDbContext> options
       , JAMServiceConfiguration configuration) : DbContext(options)
{

    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<AppRequest> AppRequests { get; set; } = default!;
    public DbSet<Application> Applications { get; set; } = default!;
    public DbSet<AppRoleMapping> AppRoleMappings { get; set; } = default!;
    public DbSet<IDPMapper> IDPMappers { get; set; } = default!;

    public async Task<bool> HasBeenProcessed(string messageId, string consumer) => await this.IdempotentConsumers.AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer);


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = configuration.DatabaseConnectionInfo.Schema;

        modelBuilder.HasDefaultSchema(schema);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JAMServiceDbContext).Assembly);

        var app = modelBuilder.Entity<Application>().HasData(
             new Application { Id = 1, Name = "JAM_POR", Description = "JUSTIN Protection Order Registry", GroupPath = "/JAM/POR", ValidIDPs = ["azuread"] });

        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = 1, Id = 1, IsRealmGroup = true, Role = "POR_READ_ONLY" });
        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = 1, Id = 2, IsRealmGroup = true, Role = "POR_READ_WRITE" });
        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = 1, Id = 3, IsRealmGroup = true, Role = "POR_DELETE_ORDER" });


        var mapping = modelBuilder.Entity<IDPMapper>().HasData(
             new IDPMapper { Id = 1, SourceRealm = "BCPS", SourceIdp = "azuread", TargetRealm = "ISB", TargetIdp = "azure-idir" });

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
