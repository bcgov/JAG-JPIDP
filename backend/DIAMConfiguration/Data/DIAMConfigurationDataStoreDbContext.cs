namespace DIAMConfiguration.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

public class DIAMConfigurationDataStoreDbContext(DbContextOptions<DIAMConfigurationDataStoreDbContext> options, IClock clock) : DbContext(options)
{
    private readonly IClock clock = clock;

    public DbSet<LoginConfig> LoginConfigs { get; set; } = default!;
    public DbSet<HostConfig> HostConfigs { get; set; } = default!;
    public DbSet<PageContent> PageContents { get; set; } = default!;
    public DbSet<UserPreference> UserPreferences { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<LoginConfig>()
            .Property(e => e.Type)
            .HasConversion(
                v => v.ToString(),
                v => (LoginOptionType)Enum.Parse(typeof(LoginOptionType), v));


        modelBuilder.Entity<LoginConfig>().HasData(
        new LoginConfig { Id = 1, Created = DateTime.Now, Idp = "ADFS", Name = "BCPS iKey", Type = LoginOptionType.BUTTON },
        new LoginConfig { Id = 2, Created = DateTime.Now, Idp = "oidcazure", Name = "BCPS IDIR", Type = LoginOptionType.BUTTON },
        new LoginConfig { Id = 3, Created = DateTime.Now, Idp = "verified", Name = "Verifiable Credentials", Type = LoginOptionType.BUTTON },
        new LoginConfig { Id = 4, Created = DateTime.Now, Idp = "bcsc", Name = "BC Services Card", Type = LoginOptionType.BUTTON },
        new LoginConfig { Id = 5, Created = DateTime.Now, Idp = "submitting_agencies", Name = "BCPS IDIR", Type = LoginOptionType.AUTOCOMPLETE, FormControl = "selectedAgency", FormList = "filteredAgencies" },
        new LoginConfig { Id = 6, Created = DateTime.Now, Idp = "azuread", Name = "BCPS Azure MFA", Type = LoginOptionType.BUTTON }
        );


        modelBuilder.Entity<HostConfig>().HasData(
        new HostConfig { Id = 1, Created = DateTime.Now, Hostname = "locahost" }
);


    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var insertedEntries = this.ChangeTracker.Entries()
                               .Where(x => x.State == EntityState.Added)
                               .Select(x => x.Entity);

        foreach (var insertedEntry in insertedEntries)
        {
            var auditableEntity = insertedEntry as BaseEntity;
            //If the inserted object is an Auditable. 
            if (auditableEntity != null)
            {
                auditableEntity.Created = DateTimeOffset.UtcNow;
            }
        }

        var modifiedEntries = this.ChangeTracker.Entries()
                   .Where(x => x.State == EntityState.Modified)
                   .Select(x => x.Entity);

        foreach (var modifiedEntry in modifiedEntries)
        {
            //If the inserted object is an Auditable. 
            var auditableEntity = modifiedEntry as BaseEntity;
            if (auditableEntity != null)
            {
                auditableEntity.Modified = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<ZonedDateTime>(x => x.HaveConversion<ZonedDateTimeConverter>());
    }

    internal class ZonedDateTimeConverter : ValueConverter<ZonedDateTime, LocalDateTime>
    {
        public ZonedDateTimeConverter() :
           base(v => v.WithZone(DateTimeZone.Utc).LocalDateTime, v => v.InUtc())
        {
        }
    }
}
