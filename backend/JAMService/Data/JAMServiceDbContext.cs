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
             new Application { Id = -1, Name = "JAM_POR", Description = "JUSTIN Protection Order Registry", GroupPath = "/JAM/POR", ValidIDPs = ["azuread"], JUSTINAppName = "POR" });

        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = -1, Id = -1, IsRealmGroup = true, Description = "Read-only: Current protection orders and expired", SourceRoles = ["POS_VIEW_ALL_USER", "POS_USER"], TargetRoles = ["POR_READ_EXPIRED_ORDERS"] });
        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = -1, Id = -2, IsRealmGroup = true, Description = "Read-only: Current protection orders only", SourceRoles = ["POS_SEL_USER", "POS_USER"], TargetRoles = ["POR_READ_VALID_ONLY"] });
        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = -1, Id = -3, IsRealmGroup = true, Description = "Regular user: Admin without remove orders permission", SourceRoles = ["POS_USER"], TargetRoles = ["POR_READ_WRITE"] });
        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = -1, Id = -4, IsRealmGroup = true, Description = "Admin with remove orders permission", SourceRoles = ["POS_USER", "POS_DEL_USER"], TargetRoles = ["POR_ADMIN_WITH_SEALING"] });
        modelBuilder.Entity<AppRoleMapping>().HasData(new AppRoleMapping { ApplicationId = -1, Id = -5, IsRealmGroup = true, Description = "Ability to seal protection orders and mark as removed", SourceRoles = ["POS_USER", "POS_REMOVE_USER"], TargetRoles = ["POR_ADMIN_WITH_SEALING"] });


        var mapping = modelBuilder.Entity<IDPMapper>().HasData(
             new IDPMapper { Id = -1, SourceRealm = "BCPS", SourceIdp = "azuread", TargetRealm = "ISB", TargetIdp = "azure-idir" });

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
